using Mntone.Nico2.Videos.Thumbnail;
using Mntone.Nico2.Videos.WatchAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Web.Http;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Diagnostics;
using System.Threading;
using System.Collections.Concurrent;
using Windows.Foundation;
using NicoPlayerHohoema.Util;
using System.Net;

namespace NicoPlayerHohoema.Models
{
	// Note: 動画のプログレッシブダウンロードのサポート
	// １．シーク位置に合わせた動画リソースの継続ダウンロード
	// ２．動画リソースのファイルへのキャッシュ処理
	// ３．キャッシュファイルを元にした再生用ストリーム
	// ４．シークによって歯抜けになった動画リソースの補完ダウンロード

	public class NicoVideoCachedStream : Util.HttpRandomAccessStream
	{
		// 動画ファイル(*.mp4)と動画ダウンロード状況ファイル(*_progress.json)を扱う


		// エコノミーとオリジナルの切り替えはここでは責任を持たない
		// 動画情報.jsonやコメント.jsonはここでは取り扱わない



		static NicoVideoCachedStream()
		{
			_CacheProgressSemaphore = new SemaphoreSlim(1, 1);
		}


		public static bool ExistOriginalQuorityVideo(string title, string videoId, StorageFolder folder)
		{
			return File.Exists(Path.Combine(folder.Path, $"{MakeVideoFileName(title, videoId)}.mp4".ToSafeFilePath()));
		}

		public static bool ExistLowQuorityVideo(string title, string videoId, StorageFolder folder)
		{
			return File.Exists(Path.Combine(folder.Path, $"{MakeVideoFileName(title, videoId)}.low.mp4".ToSafeFilePath()));
		}

		public static bool ExistIncompleteOriginalQuorityVideo(string title, string videoId, StorageFolder folder)
		{
			return File.Exists(Path.Combine(folder.Path, $"{MakeVideoFileName(title, videoId)}.mp4{IncompleteExt}".ToSafeFilePath()));
		}

		public static bool ExistIncompleteLowQuorityVideo(string title, string videoId, StorageFolder folder)
		{
			return File.Exists(Path.Combine(folder.Path, $"{MakeVideoFileName(title, videoId)}.low.mp4{IncompleteExt}".ToSafeFilePath()));
		}



		public const string IncompleteExt = ".incomplete";

		public static string MakeVideoFileName(string title, string videoid)
		{
			return $"{title} - [{videoid}]";
		}


		

		public static async Task<NicoVideoCachedStream> Create(HttpClient client, string rawVideoId, WatchApiResponse res, ThumbnailResponse thumbnailRes, StorageFolder videoSaveFolder, NicoVideoQuality quality)
		{
			// fileはincompleteか
			var videoId = res.videoDetail.id;
			var videoTitle = res.videoDetail.title.ToSafeFilePath();
			var videoFileName = MakeVideoFileName(videoTitle, videoId);
			var fileName = $"{videoFileName}.mp4";
			var fileName_low = $"{videoFileName}.low.mp4";

			var dirInfo = new DirectoryInfo(videoSaveFolder.Path);

			// 動画URLをキャッシュモードによって強制する
			string videoUrl = res.VideoUrl.AbsoluteUri;
			var isEconomy = videoUrl.EndsWith("low");

			uint streamSize = (uint)(isEconomy ? thumbnailRes.SizeLow : thumbnailRes.SizeHigh);

			StorageFile videoFile = null;

			if (quality == NicoVideoQuality.Original && isEconomy)
			{
				throw new Exception("エコノミーモードのためオリジナル画質の動画がダウンロードできません。");
			}

			if (quality == NicoVideoQuality.Original)
			{				
				videoFile = await videoSaveFolder.TryGetItemAsync(fileName) as StorageFile;
				if (videoFile == null)
				{
					videoFile = await videoSaveFolder.TryGetItemAsync(fileName + IncompleteExt) as StorageFile;
				}
			}
			else
			{
				videoFile = await videoSaveFolder.TryGetItemAsync(fileName_low) as StorageFile;

				if (videoFile == null)
				{
					videoFile = await videoSaveFolder.TryGetItemAsync(fileName_low + IncompleteExt) as StorageFile;
				}
			}




			if (videoFile == null)
			{
				// 画質モードに応じてファイルを作成、最初はファイル名に.incompleteが付く
				if (isEconomy)
				{
					videoFile = await videoSaveFolder.CreateFileAsync($"{fileName_low}{IncompleteExt}", CreationCollisionOption.ReplaceExisting);
				}
				else
				{
					videoFile = await videoSaveFolder.CreateFileAsync($"{fileName}{IncompleteExt}", CreationCollisionOption.ReplaceExisting);
				}
			}

			var stream = new NicoVideoCachedStream(client, rawVideoId, videoId, new Uri(videoUrl), videoFile, quality);

			stream.IsPremiumUser = res.IsPremium;
			stream.DownloadInterval = res.IsPremium ?
				TimeSpan.FromSeconds(BUFFER_SIZE / (float)PremiumUserDownload_kbps + 0.2) :
				TimeSpan.FromSeconds(BUFFER_SIZE / (float)IppanUserDownload_kbps + 0.2);

			stream.Size = streamSize;

			System.Diagnostics.Debug.WriteLine($"size:{stream.Size}");

			await stream.Initialize(videoSaveFolder);

			return stream;
		}


		public static async Task ClearCacheFiles(StorageFolder saveFolder, WatchApiResponse res)
		{
			var videoId = res.videoDetail.id;
			var videoTitle = res.videoDetail.title.ToSafeFilePath();
			var videoFileName = MakeVideoFileName(videoTitle, videoId);
			var filename_orig = $"{videoFileName}.mp4";
			var filename_low = $"{videoFileName}.low.mp4";

			await EnsureDeleteFile(saveFolder, filename_orig);
			await EnsureDeleteFile(saveFolder, filename_low);
			await EnsureDeleteFile(saveFolder, filename_orig + IncompleteExt);
			await EnsureDeleteFile(saveFolder, filename_low + IncompleteExt);

		}

		public static async Task ClearProgressFile(StorageFolder saveFolder, string rawVideoId)
		{
			var progressFilename = GetProgressFileName(rawVideoId);

			await EnsureDeleteFile(saveFolder, progressFilename + ".json");
			await EnsureDeleteFile(saveFolder, progressFilename + ".low.json");
		}

		private static async Task EnsureDeleteFile(StorageFolder saveFolder, string filename)
		{
			if (saveFolder.ExistFile(filename))
			{
				var file = await saveFolder.GetFileAsync(filename);
				await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
			}
		}


		public NicoVideoCachedStream(HttpClient client, string rawVideoId, string videoId, Uri uri, StorageFile file, NicoVideoQuality quality)
			: base(client, uri)
		{
			RawVideoId = rawVideoId;
			VideoId = videoId;
			CacheFile = file;
			Quality = quality;

			_Progress = null;
			_ProgressFile = null;

			_CacheWriteSemaphore = new SemaphoreSlim(1, 1);
			_DownloadTaskLock = new SemaphoreSlim(1, 1);
		}



		private async Task Initialize(StorageFolder videoSaveFolder)
		{
			try
			{
				await _CacheProgressSemaphore.WaitAsync();

				var progressFileName = GetProgressFileName(RawVideoId);
				var isLowQuality = Quality == NicoVideoQuality.Low;
				// cacheProgressFileの存在をチェックし、あれば読み込み
				var name = progressFileName + (isLowQuality ? ".low.json" : ".json");
				if (File.Exists(Path.Combine(videoSaveFolder.Path, name)))
				{
					var progressFile = await videoSaveFolder.GetFileAsync(name);
					var jsonText = await FileIO.ReadTextAsync(progressFile);

					_Progress = Newtonsoft.Json.JsonConvert.DeserializeObject<VideoCacheProgress>(jsonText);
					_ProgressFile = progressFile;
				}

				if (_Progress == null)
				{
					_Progress = new VideoCacheProgress((uint)Size);
					_ProgressFile = await videoSaveFolder.CreateFileAsync(name, CreationCollisionOption.ReplaceExisting);
				}
				
			}
			finally
			{
				_CacheProgressSemaphore.Release();
			}
		}

		public override void Dispose()
		{
			DecrementRef();

			// まだ参照がある場合は処理しない
			if (!IsNoRefarence) { return; }

			// もうクローズ済みの場合は処理しない
			if (IsClosed) { return; }


			IsClosed = true;

			var task = Task.Run(async () => 
			{
				try
				{
					await _CacheWriteSemaphore.WaitAsync().ConfigureAwait(false);
					await StopDownload().ConfigureAwait(false);
				}
				finally
				{
					_CacheWriteSemaphore.Release();
				}


				if (!IsCacheComplete)
				{
					await SaveProgress().ConfigureAwait(false);
				}

				if (!IsCacheRequested)
				{
					await CacheFile.DeleteAsync().AsTask().ConfigureAwait(false);
					CacheFile = null;
					_ProgressFile?.DeleteAsync().AsTask().ConfigureAwait(false);
					_ProgressFile = null;
				}
			});

			task.Wait();

			// stopdownload 
			base.Dispose();
		}


		#region override HttpRandomAccessStream

		public override void Seek(ulong position)
		{
			base.Seek(position);

			if (!CurrentPositionIsCached(0)
				&& _CurrentDownloadHead != position
				&& !IsClosed)
			{
				try
				{
					StartDownloadTask((uint)position).ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex.Message);
				}
			}
		}

		private IAsyncOperationWithProgress<IBuffer, uint> _ReadAsyncAction;
		public override IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
		{
			return _ReadAsyncAction = AsyncInfo.Run<IBuffer, uint>(async (cancellationToken, progress) =>
			{
				// まだキャッシュが終わってない場合は指定区間のダウンロード完了を待つ
				if (_Progress != null)
				{
					var waitCount = 0;
					while (!CurrentPositionIsCached(count))
					{
						cancellationToken.ThrowIfCancellationRequested();

						await Task.Delay(250).ConfigureAwait(false);

						Debug.Write("キャッシュ待ち...");

						if (_DownloadTask == null)
						{
							_ReadAsyncAction?.Cancel();
						}

						waitCount++;
					}

					if (waitCount != 0)
					{
						await Task.Delay(250);

						cancellationToken.ThrowIfCancellationRequested();
					}

					if (!CurrentPositionIsCached(count))
					{
						throw new Exception();
					}
				}

				cancellationToken.ThrowIfCancellationRequested();


				IInputStream videoFragmentStream = null;
				IBuffer videoFragmentBuffer = buffer;

				while (CacheFile == null)
				{
					cancellationToken.ThrowIfCancellationRequested();

					_ReadAsyncAction?.Cancel();

					await Task.Delay(250).ConfigureAwait(false);

					Debug.Write("キャンセル待ち...");
				}

				try
				{
					await _CacheWriteSemaphore.WaitAsync();

					cancellationToken.ThrowIfCancellationRequested();

					for (int i = 0; i < 3; i++)
					{
						try
						{
							using (var stream = await CacheFile.OpenReadAsync())
							{
								videoFragmentStream = stream.GetInputStreamAt(Position);

								videoFragmentBuffer = await videoFragmentStream.ReadAsync(buffer, count, options).AsTask(cancellationToken, progress);

								Debug.WriteLine($"read: {Position} + {videoFragmentBuffer.Length}");

								_CurrentPosition += videoFragmentBuffer.Length;
							}
							break;
						}
						catch
						{
							await Task.Delay(100);

							cancellationToken.ThrowIfCancellationRequested();
						}
					}

					
				}
				finally
				{
					_CacheWriteSemaphore.Release();
				}

				_ReadAsyncAction = null;

				return videoFragmentBuffer;
			});
		}


		#endregion





		public async Task Download()
		{
			if (IsCacheComplete)
			{
				return;
			}

			await StartDownloadTask(0);
		}

		public async Task StopDownload()
		{
			var stopped = await _StopDownload().ConfigureAwait(false); ;
			if (stopped)
			{
				OnCacheCanceled?.Invoke(RawVideoId);
			}
		}


		public bool IsCacheRequested
		{
			get
			{
				return _Progress.IsRequestCache;
			}
			set
			{
				_Progress.IsRequestCache = value;
			}
		}





		#region Download and Cache writing

		private async Task<bool> _StopDownload()
		{
			try
			{
				await _DownloadTaskLock.WaitAsync().ConfigureAwait(false);

				if (_DownloadTask == null)
				{
					Debug.Write("ダウンロードは既に終了");
					return true;
				}

				if (_DownloadTaskCancelToken != null
					&& _DownloadTaskCancelToken.IsCancellationRequested == true)
				{
					Debug.Write("ダウンロードキャンセルは既にリクエスト済みです");
					return true;
				}


				await Task.Delay(100).ConfigureAwait(false);

				_DownloadTaskCancelToken?.Cancel();

//				_ReadAsyncAction?.Cancel();
//				_ReadAsyncAction = null;


				Debug.Write("ダウンロードキャンセルを待機中");


				await _DownloadTask.WaitToCompelation().ConfigureAwait(false);


				_DownloadTask = null;
				_DownloadTaskCancelToken.Dispose();
				_DownloadTaskCancelToken = null;
			}
			finally
			{
				_DownloadTaskLock.Release();
			}

			return true;
		}


		private async Task StartDownloadTask(uint position)
		{
			await _StopDownload().ConfigureAwait(false);

			try
			{
				await _DownloadTaskLock.WaitAsync();
				Debug.WriteLine(VideoId + ":" + position + " からダウンロードを開始");

				_DownloadTaskCancelToken = new CancellationTokenSource();
				_DownloadTask = DownloadIncompleteData((uint)position, _DownloadTaskCancelToken.Token)
					.AsTask(_DownloadTaskCancelToken.Token);
			}
			catch (OperationCanceledException)
			{
				Debug.WriteLine("download canceled.");
			}
			finally
			{
				_DownloadTaskLock.Release();
			}

		}
		
		private IAsyncAction DownloadIncompleteData(uint offset, CancellationToken cancellationtoken)
		{
			_CurrentDownloadHead = offset;
			// TODO: 終了予定時刻の計算?

			// ダウンロードスピードを制限する
			// プレミアム = 約500kbps
			// 一般 = 約250kbps

			if (IsPremiumUser)
			{
				Debug.WriteLine("プレミアムユーザーで" + VideoId + "のダウンロードを開始。");
			}
			else
			{
				Debug.WriteLine("一般ユーザーで" + VideoId + "のダウンロードを開始。");
			}



			return Task.Run(async () =>
			{
				// シーク後の位置だけをダウンロードする
				var ranges = _Progress.EnumerateIncompleteRanges().ToList();
				var skipedRange = ranges
					.SkipWhile(x => x.Value < offset)
					.Select(x =>
					{
						if (x.Key < offset)
						{
							return new KeyValuePair<uint, uint>(offset, x.Value);
						}
						else
						{
							return x;
						}
					});

				foreach (var incompleteRange in skipedRange)
				{
					var start = incompleteRange.Key;
					var size = incompleteRange.Value - incompleteRange.Key;

					cancellationtoken.ThrowIfCancellationRequested();

					await DownloadFragment(start, size, cancellationtoken);

					cancellationtoken.ThrowIfCancellationRequested();

					await Task.Delay(1).ConfigureAwait(false);
				}

				cancellationtoken.ThrowIfCancellationRequested();

				// キャッシュが必要な場合に未完了部分のダウンロード
				if (IsCacheRequested && !_Progress.CheckComplete())
				{
					Debug.WriteLine("キャッシュ保存のためダウンロードを継続");

					ranges = _Progress.EnumerateIncompleteRanges().ToList();

					foreach (var incompleteRange in ranges)
					{
						var start = incompleteRange.Key;
						var size = incompleteRange.Value - incompleteRange.Key;

						await DownloadFragment(start, size, cancellationtoken);

						await Task.Delay(1).ConfigureAwait(false);
					}
				}

				Debug.WriteLine("done");

				await CompleteAction();
			}
			, cancellationtoken
			)
			.AsAsyncAction();
			
		}

		private async Task DownloadFragment(uint start, uint size, CancellationToken token)
		{
			// 動画ダウンロードストリームを取得
			var inputStream = await Util.ConnectionRetryUtil.TaskWithRetry(async () =>
			{
				token.ThrowIfCancellationRequested();

				try
				{
					return await base.ReadRequestAsync(start);
				}
				catch (System.ObjectDisposedException)
				{
					token.ThrowIfCancellationRequested();
					throw;
				}
				catch (System.Exception e) 
				{
					token.ThrowIfCancellationRequested();
					throw new WebException("", e);
				}
			}, retryInterval:250);

			// バッファーの最大サイズに合わせてsizeを分割してダウンロードする
			var division = size / BUFFER_SIZE;
			for (uint i = 0; i < division; i++)
			{
				if (token.IsCancellationRequested)
				{
					Debug.WriteLine("download canceled");
				}

				token.ThrowIfCancellationRequested();

				ulong head = start + i * BUFFER_SIZE;
				await DownloadAndWriteToFile(inputStream, head, BUFFER_SIZE);

				await Task.Delay(DownloadInterval);
			}

			token.ThrowIfCancellationRequested();

			// 終端のデータだけ別処理
			var finalFragmentSize = size % BUFFER_SIZE;
			if (finalFragmentSize != 0)
			{
				ulong head = start + (uint)division * BUFFER_SIZE;
				await DownloadAndWriteToFile(inputStream, head, finalFragmentSize);

				await Task.Delay(DownloadInterval);
			}

		}

		private async Task DownloadAndWriteToFile(IInputStream inputStream, ulong head, uint readSize)
		{
			Array.Clear(RawBuffer, 0, RawBuffer.Length);
			
			var resultBuffer = await inputStream.ReadAsync(DownloadBuffer, readSize, InputStreamOptions.None).AsTask();
			await WriteToVideoFile(head, resultBuffer);

			RecordProgress((uint)head, resultBuffer.Length);
			await SaveProgress();

			OnCacheProgress?.Invoke(RawVideoId, Quality, (uint)Size, _Progress.BufferedSize());

			Debug.WriteLine($"download:{head}~{head + resultBuffer.Length}");
		}

		private async Task WriteToVideoFile(ulong position, IBuffer buffer)
		{
			try
			{
				await _CacheWriteSemaphore.WaitAsync().ConfigureAwait(false);

				if (CacheFile == null)
				{
					return;
				}

				using (var stream = await CacheFile.OpenAsync(FileAccessMode.ReadWrite))
				using (var writeStream = stream.GetOutputStreamAt(position))
				{
					await writeStream.WriteAsync(buffer).AsTask().ConfigureAwait(false);
				}

			}
			finally
			{
				_CacheWriteSemaphore.Release();
			}
		}

		private async Task CompleteAction()
		{
			// データが完全にダウンロードできたときの処理
			if (_Progress.CheckComplete())
			{

				await SaveProgress(true);

				try
				{
					// 動画ファイル名から.incompleteを削除するようリネーム
					if (CacheFile.Name.Contains((".incomplete")))
					{
						var pos = CacheFile.Name.IndexOf(".incomplete");
						var name = CacheFile.Name.Remove(pos);
						await _CacheWriteSemaphore.WaitAsync();

						var path = Path.Combine(Path.GetDirectoryName(CacheFile.Path), name);
						if (File.Exists(path))
						{
							var alreadFile = await StorageFile.GetFileFromPathAsync(path);
							await alreadFile.DeleteAsync();
						}
						await CacheFile.RenameAsync(name);
					}
				}
				finally
				{
					_CacheWriteSemaphore.Release();
				}


				Debug.WriteLine($"{VideoId} is download done.");

				OnCacheComplete?.Invoke(RawVideoId);

				try
				{
					await _DownloadTaskLock.WaitAsync().ConfigureAwait(false);
					_DownloadTask = null;
					_DownloadTaskCancelToken.Dispose();
					_DownloadTaskCancelToken = null;
				}
				finally
				{
					_DownloadTaskLock.Release();
				}
			}
		}



		#endregion



		#region Progress management

		// TODO: なぜRawVideoIdを使っているの？
		private static string GetProgressFileName(string rawVideoId)
		{
			return $"{rawVideoId}_progress";
		}


		private void RecordProgress(ulong position, uint count)
		{
			if (_Progress == null)
			{
				throw new Exception("キャッシュ済みの状態でProgressの記録は出来ません。");
			}
			try
			{
				_CacheProgressSemaphore.Wait();
				_Progress.Update((uint)position, count);
			}
			finally
			{
				_CacheProgressSemaphore.Release();
			}
		}

		private async Task SaveProgress(bool ensureWrite = false)
		{
			if (_Progress == null) { return; }

			try
			{
				await _CacheProgressSemaphore.WaitAsync().ConfigureAwait(false);

				var writeComplete = false;
				while (!writeComplete)
				{
					if (_ProgressFile == null)
					{
						return;
					}

					try
					{
						await FileIO.WriteTextAsync(_ProgressFile, Newtonsoft.Json.JsonConvert.SerializeObject(_Progress));
						break;
					}
					catch {  }

					if (!ensureWrite) break;
				}
			}
			finally
			{
				_CacheProgressSemaphore.Release();
			}
		}

		private bool CurrentPositionIsCached(uint length)
		{
			try
			{
				_CacheProgressSemaphore.Wait();

				// Progressがあればキャッシュ済み範囲か取得
				// なければキャッシュ済み(true)を返す
				return _Progress?
					.IsCachedRange((uint)_CurrentPosition, length)
					?? true;
			}
			finally
			{
				_CacheProgressSemaphore.Release();
			}
		}


		public uint GetDownloadProgress()
		{
			var remain = _Progress.RemainSize();
			return (uint)Size - remain;
		}

		
		public bool IsCacheComplete
		{
			get
			{
				return _Progress.CheckComplete();
			}
		}


		#endregion



		#region Cache Download Event

		public event Action<string> OnCacheComplete;
		public event Action<string> OnCacheCanceled;
		public event Action<string, NicoVideoQuality, uint, uint> OnCacheProgress;


		#endregion


		#region RefCount


		public void IncrementRef()
		{
			RefCount++;
		}

		private void DecrementRef()
		{
			RefCount--;
		}

		private bool IsNoRefarence
		{
			get
			{
				return RefCount <= 0;
			}
		}

		#endregion



		private IAsyncOperationWithProgress<IBuffer, uint> _CurrentOperation;

		const uint MediaElementBufferSize = 262144;

		const uint PremiumUserDownload_kbps = 262144 * 2;
		const uint IppanUserDownload_kbps = 262144;

		const uint BUFFER_SIZE = 262144 / 4;
		byte[] _RawBuffer;
		byte[] RawBuffer
		{
			get
			{
				return _RawBuffer
					?? (_RawBuffer = new byte[BUFFER_SIZE]);
			}
		}

		IBuffer _DownloadBuffer;
		IBuffer DownloadBuffer
		{
			get
			{
				return _DownloadBuffer
					?? (_DownloadBuffer = _RawBuffer.AsBuffer());
			}
		}

		private uint _CurrentDownloadHead;
		private Task _DownloadTask;
		private CancellationTokenSource _DownloadTaskCancelToken;
		private SemaphoreSlim _DownloadTaskLock;


		private VideoCacheProgress _Progress;
		private StorageFile _ProgressFile;
		private static SemaphoreSlim _CacheProgressSemaphore;


		public string RawVideoId { get; private set; }
		public string VideoId { get; private set; }
		public StorageFile CacheFile { get; private set; }
		private SemaphoreSlim _CacheWriteSemaphore;


		public NicoVideoQuality Quality { get; private set; }
		public bool IsPremiumUser { get; private set; }
		public TimeSpan DownloadInterval { get; private set; }


		public bool IsClosed { get; private set; }

		public int RefCount { get; private set; }
	}

	
	public class VideoCacheProgress
	{
		private static bool _ValueIsInsideRange(uint val, uint rangeStart, uint rangeEnd)
		{
			return rangeStart <= val && val <= rangeEnd;
		}

		private static bool _ValueIsInsideRange(uint val, ref KeyValuePair<uint, uint> range)
		{
			return _ValueIsInsideRange(val, range.Key, range.Value);
		}


		public VideoCacheProgress(uint size)
		{
			Size = size;
			CachedRanges = new SortedDictionary<uint, uint>();
		}

		

		private void _InnerUpdate(uint position, uint length)
		{
			if (Size < position) { throw new ArgumentOutOfRangeException(); }
			if (length == 0) { return; }

			var posEnd = position + length;

			bool isMerged = false;
			bool isCollideCachedRange = false;

			for (int index = 0; index < CachedRanges.Count; ++index)
			{
				var pair = CachedRanges.ElementAt(index);

				if (pair.Key == position && pair.Value == posEnd)
				{
					isCollideCachedRange = true;
					continue;
				}

				// startとの
				var isStartInside = _ValueIsInsideRange(position, ref pair);
				var isEndInside = _ValueIsInsideRange(posEnd, ref pair);

				if (isStartInside && isEndInside)
				{
					// 範囲内だがすでにキャッシュ済みのため更新は不要
					isCollideCachedRange = true;
					break;
				}
				else if (isStartInside || isEndInside)
				{
					// どちらかの範囲内
					var minStart = Math.Min(position, pair.Key);
					var maxEnd = Math.Max(posEnd, pair.Value);
					
					CachedRanges.Remove(pair.Key);

					// 後続のアイテムがさらにマージ可能な場合をチェック
					if (CachedRanges.ContainsKey(maxEnd))
					{
						// 後続アイテムの終端位置
						var end = CachedRanges[maxEnd];

						// 後続アイテム削除
						CachedRanges.Remove(maxEnd);

						// 再登録する終端位置を修正
						maxEnd = end;
					}

					CachedRanges.Add(minStart, maxEnd);

					isCollideCachedRange = true;
					isMerged = true;
					break;
				}
			}

			// 登録済みキャッシュにマージされない場合は新規登録
			if (!isCollideCachedRange)
			{
				CachedRanges.Add(position, posEnd);
			}

			if (isMerged)
			{
				_InnerUpdate(position, length);
			}
		}

		public void Update(uint position, uint length)
		{
			_InnerUpdate(position, length);
		}


		public bool IsCachedRange(uint head, uint length)
		{
			var tail = head + length;
			if (tail > Size)
			{
				tail = Size;
			}

			foreach (var range in CachedRanges)
			{
				if (_ValueIsInsideRange(head, range.Key, range.Value) &&
					_ValueIsInsideRange(tail, range.Key, range.Value))
				{
					return true;
				}
			}

			return false;
		}



		public IEnumerable<KeyValuePair<uint, uint>> EnumerateIncompleteRanges()
		{
			uint nextIncompleteRangeStart = 0;
			
			if (CachedRanges.Count == 0)
			{
				yield return new KeyValuePair<uint, uint>(0, Size);
			}
			else
			{
				foreach (var range in CachedRanges)
				{
					if (nextIncompleteRangeStart < range.Key)
					{
						yield return new KeyValuePair<uint, uint>(nextIncompleteRangeStart, range.Key);
					}
					nextIncompleteRangeStart = range.Value;
				}

				if (nextIncompleteRangeStart < Size)
				{
					yield return new KeyValuePair<uint, uint>(nextIncompleteRangeStart, Size);
				}
			}

		}

		public uint BufferedSize()
		{
			uint bufferedSize = 0;
			foreach (var range in CachedRanges)
			{
				bufferedSize += range.Value - range.Key;
			}

			return bufferedSize;
		}

		public uint RemainSize()
		{
			uint remain = 0;
			foreach (var range in EnumerateIncompleteRanges())
			{
				remain += range.Value - range.Key;
			}

			return remain;
		}

		public bool CheckComplete()
		{
			if (CachedRanges.Count == 1)
			{
				// すべてのデータがダウンロードされた時
				// キャッシュ済み区間が0~Sizeに収束する
				var range = CachedRanges.ElementAt(0);
				return range.Key == 0 && range.Value == Size;
			}
			else
			{
				return false;
			}
		}

		public uint Size { get; set; }


		/// <summary>
		/// key = start, value = end
		/// valueには長さではなく終端の絶対位置を入れる
		/// </summary>
		public IDictionary<uint, uint> CachedRanges { get; private set; }


		public bool IsRequestCache { get; set; }

	}
}
