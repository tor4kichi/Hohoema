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

namespace NicoPlayerHohoema.Models
{
	// Note: ダウンロードタスクと再生の管理
	// 

	public class NicoVideoCachedStream : Util.HttpRandomAccessStream
	{
		// エコノミーとオリジナルの切り替えはここでは責任を持たない
		// 動画情報.jsonやコメント.jsonはここでは取り扱わない

		public static async Task<NicoVideoCachedStream> Create(HttpClient client, WatchApiResponse res, StorageFolder videoSaveFolder, NicoVideoCacheMode cacheMode)
		{
			// fileはincompleteか
			var videoName = res.videoDetail.title;
			var fileName = videoName + ".mp4";
			var fileName_low = res.videoDetail.title + ".low.mp4";
			const string IncompleteExt = ".incomplete";
			var progressFileName = res.videoDetail.id + "_progress";

			var dirInfo = new DirectoryInfo(videoSaveFolder.Path);

			var relatedVideoFiles = dirInfo.EnumerateFiles($"{videoName}.*");

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
					var incompletedNormalVideo = relatedVideoFiles.SingleOrDefault(x => x.FullName.EndsWith(".mp4.incomplete"));
					if (incompletedNormalVideo != null)
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
					videoFile = await videoSaveFolder.CreateFileAsync(fileName_low + IncompleteExt);
				}
				else
				{
					videoFile = await videoSaveFolder.CreateFileAsync(fileName + IncompleteExt);
				}
			}


			var stream = new NicoVideoCachedStream(client, res.videoDetail.id, new Uri(videoUrl), videoFile, cacheMode);

			stream.Quority = isEconomy ? NicoVideoQuority.Low : NicoVideoQuority.Original;
			stream.IsPremiumUser = res.IsPremium;
			stream.DownloadInterval = res.IsPremium ?
				TimeSpan.FromSeconds((float)PremiumUserDownload_kbps / BUFFER_SIZE) :
				TimeSpan.FromSeconds((float)IppanUserDownload_kbps / BUFFER_SIZE);

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

		public static bool ExistOriginalQuorityVideo(WatchApiResponse res, StorageFolder folder)
		{
			var fileName = res.videoDetail.title + ".mp4";

			return File.Exists(Path.Combine(folder.Path, fileName));
		}

		public static bool ExistLowQuorityVideo(WatchApiResponse res, StorageFolder folder)
		{
			var fileName = res.videoDetail.title + ".low.mp4";

			return File.Exists(Path.Combine(folder.Path, fileName));
		}

		public NicoVideoCachedStream(HttpClient client, string videoId, Uri uri, StorageFile file, NicoVideoCacheMode cacheMode)
			: base(client, uri)
		{
			VideoId = videoId;
			CacheFile = file;
			CacheMode = cacheMode;

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
					var task = StopDownload();
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


				var position = _CurrentPosition;

				// まだキャッシュが終わってない場合は指定区間のダウンロード完了を待つ
				if (Progress != null)
				{
					while (!CurrentPositionIsCached(count))
					{
						await Task.Delay(100).ConfigureAwait(false);
					}
				}

				await _CacheWriteSemaphore.WaitAsync().ConfigureAwait(false);

				var stream = await CacheFile.OpenAsync(FileAccessMode.Read).AsTask().ConfigureAwait(false);
				resultStream = stream.GetInputStreamAt(Position);

				var result = await resultStream.ReadAsync(buffer, count, options).AsTask(cancellationToken, progress).ConfigureAwait(false);

				resultStream.Dispose();

				_CurrentPosition += result.Length;

				_CacheWriteSemaphore.Release();

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

			await StopDownload();

			_DownloadTaskCancelToken = new CancellationTokenSource();
			_DownloadTask = DownloadIncompleteData().AsTask(_DownloadTaskCancelToken.Token);
		}

		public async Task StopDownload()
		{
			if (_DownloadTask != null)
			{
				_DownloadTaskCancelToken.Cancel();

				while(!_DownloadTask.IsCanceled && !_DownloadTask.IsCompleted && !_DownloadTask.IsFaulted)
				{
					await Task.Delay(100);
				}

				_DownloadTask = null;
				_DownloadTaskCancelToken = null;
			}
		}

	

		
		private IAsyncAction DownloadIncompleteData(uint offset = 0)
		{
			// TODO: ストリーミング再生と動作が競合する

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


			Debug.WriteLine($"画質:{Quority.ToString()}");
			
			
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
				if (CacheMode != NicoVideoCacheMode.NoCache && !Progress.CheckComplete())
				{
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

			for (int i = 0; i < division; i++)
			{
				if (token.IsCancellationRequested)
				{
					Debug.WriteLine("download canceled");
				}

				token.ThrowIfCancellationRequested();

				ulong head = start + (uint)i * BUFFER_SIZE;

				await DownloadAndWriteToFile(head, BUFFER_SIZE);
			}

			// 終端のデータだけ別処理
			var mod = size % BUFFER_SIZE;
			{
				ulong head = start + (uint)division * BUFFER_SIZE;
				await DownloadAndWriteToFile(head, mod);
			}

		}

		private async Task DownloadAndWriteToFile(ulong head, uint readSize)
		{
			Array.Clear(RawBuffer, 0, RawBuffer.Length);

			var inputstream = await base.ReadRequestAsync(head).ConfigureAwait(false);
			var resultBuffer = await inputstream.ReadAsync(DownloadBuffer, readSize, InputStreamOptions.None).AsTask().ConfigureAwait(false);

			await WriteToVideoFile(head, resultBuffer).ConfigureAwait(false);

			Progress.Update((uint)head, resultBuffer.Length);

			await SaveProgress().ConfigureAwait(false); ;

			Debug.WriteLine($"download:{head}~{head + resultBuffer.Length}");

			await Task.Delay(DownloadInterval).ConfigureAwait(false);
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
					await ProgressFile.DeleteAsync().AsTask().ConfigureAwait(false);
					ProgressFile = null;
				}

				// 動画ファイル名から.incompleteを削除するようリネーム
				if (CacheFile.Name.Contains((".incomplete")))
				{
					var pos = CacheFile.Name.IndexOf(".incomplete");
					var name = CacheFile.Name.Remove(pos);
					await CacheFile.RenameAsync(name).AsTask().ConfigureAwait(false);
				}

				Debug.WriteLine($"{VideoId} is download done.");

				OnCacheComplete?.Invoke(VideoId);
			}
		}


		public override void Dispose()
		{
			base.Dispose();

			if (!IsCacheComplete)
			{
				SaveProgress();
			}

			if (CacheMode == NicoVideoCacheMode.NoCache)
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

		public string VideoId { get; private set; }
		public StorageFile CacheFile { get; private set; }

		public VideoCacheProgress Progress { get; private set; }
		public StorageFile ProgressFile { get; private set; }

		public NicoVideoCacheMode CacheMode { get; private set; }
		public NicoVideoQuority Quority { get; private set; }
		public bool IsPremiumUser { get; private set; }
		public TimeSpan DownloadInterval { get; private set; }


		const uint PremiumUserDownload_kbps = 262144 * 2;
		const uint IppanUserDownload_kbps = 262144;


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

			bool isNeedMoreUpdate = false;
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
					CachedRanges.Add(minStart, maxEnd);

					isCollideCachedRange = true;
					isNeedMoreUpdate = true;
					break;
				}
			}

			// 登録済みキャッシュにマージされない場合は新規登録
			if (!isCollideCachedRange)
			{
				CachedRanges.Add(position, posEnd);
			}

			if (isNeedMoreUpdate)
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



	public enum NicoVideoCacheMode
	{
		Auto,
		Original,
		Low,
		NoCache,
	}
}
