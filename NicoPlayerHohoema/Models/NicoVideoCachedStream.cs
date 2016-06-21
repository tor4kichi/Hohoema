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
		const string IncompleteExt = ".incomplete";

		public static string MakeVideoFileName(string title, string videoid)
		{
			return $"{title} - [{videoid}]";
		}


		public static async Task<NicoVideoCachedStream> Create(HttpClient client, string rawVideoId, WatchApiResponse res, StorageFolder videoSaveFolder, NicoVideoQuality quality, bool isCache)
		{
			// fileはincompleteか
			var videoId = res.videoDetail.id;
			var videoTitle = res.videoDetail.title.ToSafeFilePath();
			var videoFileName = MakeVideoFileName(res.videoDetail.title, res.videoDetail.id);
			var fileName = $"{videoFileName}.mp4";
			var fileName_low = $"{videoFileName}.low.mp4";
			var progressFileName = $"{videoId}_progress";

			var dirInfo = new DirectoryInfo(videoSaveFolder.Path);

			var relatedVideoFiles = dirInfo.EnumerateFiles($"{videoFileName}.*");

			// 動画URLをキャッシュモードによって強制する
			string videoUrl = res.VideoUrl.AbsoluteUri;
			var isEconomy = videoUrl.EndsWith("low");

			StorageFile videoFile = null;


			var completedNormalVideo = relatedVideoFiles.SingleOrDefault(x => x.FullName.EndsWith(".mp4"));
			if (completedNormalVideo != null)
			{
				// 
				videoFile = await StorageFile.GetFileFromPathAsync(completedNormalVideo.FullName);
			}

			if (videoFile == null)
			{
				var completedLowVideo = relatedVideoFiles.SingleOrDefault(x => x.FullName.EndsWith(".low.mp4"));
				if (completedLowVideo != null)
				{
					videoFile = await StorageFile.GetFileFromPathAsync(completedLowVideo.FullName);
				}
			}



			if (videoFile == null)
			{
				if (!isEconomy)
				{
					var incompletedNormalVideo = new FileInfo(Path.Combine(videoSaveFolder.Path, fileName + IncompleteExt));
					if (incompletedNormalVideo != null && incompletedNormalVideo.Exists)
					{
						videoFile = await StorageFile.GetFileFromPathAsync(incompletedNormalVideo.FullName);
					}
				}
				else
				{
					var incompletedLowVideo = relatedVideoFiles.SingleOrDefault(x => x.FullName.EndsWith(".low.mp4.incomplete"));
					if (incompletedLowVideo != null)
					{
						videoFile = await StorageFile.GetFileFromPathAsync(incompletedLowVideo.FullName);
					}

				}
			}
			
			if (videoFile == null)
			{
				// 画質モードに応じてファイルを作成、最初はファイル名に.incompleteが付く
				if (isEconomy)
				{
					videoFile = await videoSaveFolder.CreateFileAsync($"{fileName_low}{IncompleteExt}");
				}
				else
				{
					videoFile = await videoSaveFolder.CreateFileAsync($"{fileName}{IncompleteExt}");
				}
			}


			var stream = new NicoVideoCachedStream(client, rawVideoId, videoId, new Uri(videoUrl), videoFile, quality, isCache);

			stream.Quality = quality;
			stream.IsPremiumUser = res.IsPremium;
			stream.DownloadInterval = res.IsPremium ?
				TimeSpan.FromSeconds(BUFFER_SIZE / (float)PremiumUserDownload_kbps) :
				TimeSpan.FromSeconds(BUFFER_SIZE / (float)IppanUserDownload_kbps);

			await stream.ReadRequestAsync(0).ConfigureAwait(false);

			System.Diagnostics.Debug.WriteLine($"size:{stream.Size}");

			// ProgressFileの解決
			if (videoFile != null && videoFile.FileType == IncompleteExt)
			{
				// cacheProgressFileの存在をチェックし、あれば読み込み
				var name = progressFileName + (isEconomy ? ".low.json" : ".json");
				if (File.Exists(Path.Combine(videoSaveFolder.Path, name)))
				{
					var progressFile = await videoSaveFolder.GetFileAsync(name);
					var jsonText = await FileIO.ReadTextAsync(progressFile);

					stream.Progress = Newtonsoft.Json.JsonConvert.DeserializeObject<VideoCacheProgress>(jsonText);
					stream.ProgressFile = progressFile;
				}
				else
				{
					stream.Progress = new VideoCacheProgress((uint)stream.Size);
					stream.ProgressFile = await videoSaveFolder.CreateFileAsync(name);
					await stream.SaveProgress();
				}
			}

			return stream;
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


		public NicoVideoCachedStream(HttpClient client, string rawVideoId, string videoId, Uri uri, StorageFile file, NicoVideoQuality quality, bool isCache)
			: base(client, uri)
		{
			RawVideoId = rawVideoId;
			VideoId = videoId;
			CacheFile = file;
			Quality = quality;
			IsRequireCache = isCache;

			Progress = null;
			ProgressFile = null;

			_CacheWriteSemaphore = new SemaphoreSlim(1, 1);
			_CacheProgressSemaphore = new SemaphoreSlim(1, 1);
		}

		public uint GetDownloadProgress()
		{
			var remain = Progress.RemainSize();
			return (uint)Size - remain;
		}
		

		public override void Seek(ulong position)
		{
			base.Seek(position);

			if (!CurrentPositionIsCached(0))
			{
				if (_DownloadTask != null)
				{
					var task = _StopDownload();
					task.Wait();
				}

				Debug.WriteLine(VideoId + ":" + position +" からダウンロードを再開");

				_DownloadTaskCancelToken = new CancellationTokenSource();
				_DownloadTask = DownloadIncompleteData((uint)position).AsTask(_DownloadTaskCancelToken.Token);
			}

		}

		public override Windows.Foundation.IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
		{
			return AsyncInfo.Run<IBuffer, uint>(async (cancellationToken, progress) =>
			{
				IInputStream resultStream = null;


				// まだキャッシュが終わってない場合は指定区間のダウンロード完了を待つ
				if (Progress != null)
				{
					while (!CurrentPositionIsCached(count))
					{
						cancellationToken.ThrowIfCancellationRequested();

						await Task.Delay(100).ConfigureAwait(false);
					}
				}

				cancellationToken.ThrowIfCancellationRequested();

				await _CacheWriteSemaphore.WaitAsync().ConfigureAwait(false);

				var stream = await CacheFile.OpenAsync(FileAccessMode.Read).AsTask().ConfigureAwait(false);
				resultStream = stream.GetInputStreamAt(Position);

				var result = await resultStream.ReadAsync(buffer, count, options).AsTask(cancellationToken, progress).ConfigureAwait(false);

				_CurrentPosition += result.Length;

				_CacheWriteSemaphore.Release();

				resultStream.Dispose();

				return result;
			});
		}


		private async Task WriteToVideoFile(ulong position, IBuffer buffer)
		{
			try
			{
				await _CacheWriteSemaphore.WaitAsync().ConfigureAwait(false);

				using (var stream = await CacheFile.OpenAsync(FileAccessMode.ReadWrite).AsTask().ConfigureAwait(false))
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


		private bool CurrentPositionIsCached(uint length)
		{
			try
			{
				_CacheProgressSemaphore.Wait();

				// Progressがあればキャッシュ済み範囲か取得
				// なければキャッシュ済み(true)を返す
				return Progress?
					.IsCachedRange((uint)_CurrentPosition, length) 
					?? true;
			}
			finally
			{
				_CacheProgressSemaphore.Release();
			}
		}


		private void RecordProgress(ulong position, uint count)
		{
			if (Progress == null)
			{
				throw new Exception("キャッシュ済みの状態でProgressの記録は出来ません。");
			}
			try
			{
				_CacheProgressSemaphore.Wait();
				Progress.Update((uint)position, count);
			}
			finally
			{
				_CacheProgressSemaphore.Release();
			}
			
		}

		private async Task SaveProgress(bool ensureWrite = false)
		{
			if (Progress == null) { return; }

			try
			{
				await _CacheProgressSemaphore.WaitAsync().ConfigureAwait(false);

				var writeComplete = false;
				while (!writeComplete)
				{
					try
					{
						await FileIO.WriteTextAsync(ProgressFile, Newtonsoft.Json.JsonConvert.SerializeObject(Progress)).AsTask().ConfigureAwait(false);
					}
					catch { }

					if (!ensureWrite) break;
				}
			}
			finally
			{
				_CacheProgressSemaphore.Release();
			}
		}

		public async Task Download()
		{
			if (IsCacheComplete)
			{
				return;
			}

			await _StopDownload();

			_DownloadTaskCancelToken = new CancellationTokenSource();
			_DownloadTask = DownloadIncompleteData().AsTask(_DownloadTaskCancelToken.Token);
		}

		public async Task StopDownload()
		{
			var stopped = await _StopDownload();
			if (stopped)
			{
				OnCacheCanceled?.Invoke(RawVideoId);
			}
		}

		private async Task<bool> _StopDownload()
		{
			if (_DownloadTask != null)
			{
				_DownloadTaskCancelToken.Cancel();

				while(!_DownloadTask.IsCanceled && !_DownloadTask.IsCompleted && !_DownloadTask.IsFaulted)
				{
					await Task.Delay(10);
				}

				_DownloadTask = null;
				_DownloadTaskCancelToken = null;

				return true;
			}

			return false;
		}

	

		
		private IAsyncAction DownloadIncompleteData(uint offset = 0)
		{
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



			return AsyncInfo.Run(async (cancellationtoken) => 
			{
				// シーク後の位置だけをダウンロードする
				var ranges = Progress.EnumerateIncompleteRanges().ToList();
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

					await DownloadFragment(start, size, cancellationtoken);
				}

				// キャッシュが必要な場合に未完了部分のダウンロード
				if (IsRequireCache && !Progress.CheckComplete())
				{
					Debug.WriteLine("キャッシュ保存のためダウンロードを継続");

					ranges = Progress.EnumerateIncompleteRanges().ToList();

					foreach (var incompleteRange in ranges)
					{
						var start = incompleteRange.Key;
						var size = incompleteRange.Value - incompleteRange.Key;

						await DownloadFragment(start, size, cancellationtoken);
					}
				}

				await CompleteAction().ConfigureAwait(false);
			});
		}

		private async Task DownloadFragment(uint start, uint size, CancellationToken token)
		{
			// バッファーの最大サイズに合わせてsizeを分割してダウンロードする
			var division = size / BUFFER_SIZE;

			var inputStream = await Util.ConnectionRetryUtil.TaskWithRetry(async () =>
			{
				try
				{
					return await base.ReadRequestAsync(start).ConfigureAwait(false);
				}
				catch (System.Exception e) 
				{
					throw new WebException("", e);
				}
			});

			for (int i = 0; i < division; i++)
			{
				if (token.IsCancellationRequested)
				{
					Debug.WriteLine("download canceled");
				}

				token.ThrowIfCancellationRequested();

				ulong head = start + (uint)i * BUFFER_SIZE;
				await DownloadAndWriteToFile(inputStream, head, BUFFER_SIZE);

				await Task.Delay(DownloadInterval).ConfigureAwait(false);
			}

			// 終端のデータだけ別処理
			var mod = size % BUFFER_SIZE;
			if (mod != 0)
			{
				ulong head = start + (uint)division * BUFFER_SIZE;
				var inputstream = await base.ReadRequestAsync(head).ConfigureAwait(false);
				await DownloadAndWriteToFile(inputStream, head, mod);

				await Task.Delay(DownloadInterval).ConfigureAwait(false);
			}

		}

		private async Task DownloadAndWriteToFile(IInputStream inputStream, ulong head, uint readSize)
		{
			Array.Clear(RawBuffer, 0, RawBuffer.Length);

			var resultBuffer = await inputStream.ReadAsync(DownloadBuffer, readSize, InputStreamOptions.None).AsTask().ConfigureAwait(false);

			await WriteToVideoFile(head, resultBuffer).ConfigureAwait(false);

			Progress.Update((uint)head, resultBuffer.Length);

			await SaveProgress().ConfigureAwait(false);

			OnCacheProgress?.Invoke(RawVideoId, Quality, (uint)Size, Progress.BufferedSize());

			Debug.WriteLine($"download:{head}~{head + resultBuffer.Length}");

		}



		private async Task CompleteAction()
		{
			// データが完全にダウンロードできたときの処理
			if (Progress.CheckComplete())
			{
				Progress = null;

				// Progressファイルの削除
				if (ProgressFile != null)
				{
					try
					{
						await _CacheProgressSemaphore.WaitAsync().ConfigureAwait(false);
						await ProgressFile.DeleteAsync().AsTask().ConfigureAwait(false);
						ProgressFile = null;
					}
					finally
					{
						_CacheProgressSemaphore.Release();
					}
				}

				// 動画ファイル名から.incompleteを削除するようリネーム
				if (CacheFile.Name.Contains((".incomplete")))
				{
					var pos = CacheFile.Name.IndexOf(".incomplete");
					var name = CacheFile.Name.Remove(pos);
					try
					{
						await _CacheWriteSemaphore.WaitAsync().ConfigureAwait(false);
						await CacheFile.RenameAsync(name).AsTask().ConfigureAwait(false);
					}
					finally
					{
						_CacheWriteSemaphore.Release();
					}
				}

				Debug.WriteLine($"{VideoId} is download done.");

				OnCacheComplete?.Invoke(RawVideoId);
			}
		}


		public override void Dispose()
		{
			base.Dispose();

			StopDownload();

			if (!IsCacheComplete)
			{
				SaveProgress();
			}

			if (!IsRequireCache)
			{
				CacheFile.DeleteAsync().AsTask().ConfigureAwait(false);
				ProgressFile?.DeleteAsync().AsTask().ConfigureAwait(false);
			}
		}


		public bool IsCacheComplete
		{
			get
			{
				return Progress == null;
			}
		}

		const uint BUFFER_SIZE = 262144;
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

		private CancellationTokenSource _DownloadTaskCancelToken;
		private Task _DownloadTask;

		private SemaphoreSlim _CacheProgressSemaphore;

		private SemaphoreSlim _CacheWriteSemaphore;

		public event Action<string> OnCacheComplete;
		public event Action<string> OnCacheCanceled;
		public event Action<string, NicoVideoQuality, uint, uint> OnCacheProgress;

		public string RawVideoId { get; private set; }
		public string VideoId { get; private set; }
		public StorageFile CacheFile { get; private set; }

		public VideoCacheProgress Progress { get; private set; }
		public StorageFile ProgressFile { get; private set; }

		public bool IsRequireCache { get; private set; }
		public NicoVideoQuality Quality { get; private set; }
		public bool IsPremiumUser { get; private set; }
		public TimeSpan DownloadInterval { get; private set; }


		const uint PremiumUserDownload_kbps = 262144 * 2;
		const uint IppanUserDownload_kbps = 200000;


	}

	public class InMemoryBuffer
	{
		public InMemoryBuffer(uint bufferSize = 266514)
		{
			Buffer = new byte[bufferSize].AsBuffer();
			Position = 0;
			Count = 0;
			IsCached = false;
		}

		public IBuffer Buffer { get; set; }
		public uint Position { get; set; }
		public uint Count { get; set; }
		public bool IsCached { get; set; }
	}

	
	public class VideoCacheProgress
	{
		public VideoCacheProgress(uint size)
		{
			Size = size;
			CachedRanges = new SortedDictionary<uint, uint>();
		}

		private bool _ValueIsInsideRange(uint val, uint rangeStart, uint rangeEnd)
		{
			return rangeStart <= val && val <= rangeEnd;
		}

		private bool _ValueIsInsideRange(uint val, ref KeyValuePair<uint, uint> range)
		{
			return _ValueIsInsideRange(val, range.Key, range.Value);
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


		public bool IsCachedRange(uint position, uint length)
		{
			foreach (var range in CachedRanges)
			{
				if (_ValueIsInsideRange(position, range.Key, range.Value) &&
					_ValueIsInsideRange(position + length, range.Key, range.Value))
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

	}
}
