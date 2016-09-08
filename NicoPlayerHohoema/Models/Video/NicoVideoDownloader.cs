using Mntone.Nico2.Videos.Thumbnail;
using Mntone.Nico2.Videos.WatchAPI;
using NicoPlayerHohoema.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Web.Http;

namespace NicoPlayerHohoema.Models
{
	// Note: 動画のプログレッシブダウンロードのサポート
	// １．シーク位置に合わせた動画リソースの継続ダウンロード
	// ２．動画リソースのファイルへのキャッシュ処理
	// ３．キャッシュファイルを元にした再生用ストリーム
	// ４．シークによって歯抜けになった動画リソースの補完ダウンロード

	public delegate void DownloadProgressEvnetHandler(string rawVideoId, NicoVideoQuality quality, uint progressHead, uint length);

	public class NicoVideoDownloader : Util.HttpRandomAccessStream
	{
		public static readonly TimeSpan DownloadDelay = TimeSpan.FromMilliseconds(50);


		public uint CurrentDownloadHead { get; private set; }
		private Task _DownloadTask;
		private CancellationTokenSource _DownloadTaskCancelToken;
		private SemaphoreSlim _DownloadTaskLock;

		public DividedQualityNicoVideo DividedQualityNicoVideo { get; private set; }

		public SemaphoreSlim _ProgressReadWriteLock;
		public VideoDownloadProgress DownloadProgress { get; private set; }



		public StorageFile CacheFile { get; private set; }
		private SemaphoreSlim _CacheWriteSemaphore;


		public bool IsPremiumUser { get; private set; }
		public TimeSpan DownloadInterval { get; private set; }


		public bool IsClosed { get; private set; }

		public string RawVideoId
		{
			get
			{
				return DividedQualityNicoVideo.RawVideoId;
			}
		}

		public NicoVideoQuality Quality
		{
			get { return DividedQualityNicoVideo.Quality; }
		}


		#region Cache Download Event

		public event Action<string> OnCacheComplete;
		public event Action<string> OnCacheCanceled;
		public event DownloadProgressEvnetHandler OnCacheProgress;


		#endregion


		const uint MediaElementBufferSize = 262144;

		const uint PremiumUserDownload_kbps = 262144 * 2;
		const uint IppanUserDownload_kbps = 262144;


		public NicoVideoDownloader(DividedQualityNicoVideo qualityNicoVideo, HttpClient client,  WatchApiResponse watchApiRes, StorageFile cacheFile)
			: base(client, watchApiRes.VideoUrl)
		{
			DividedQualityNicoVideo = qualityNicoVideo;
			IsPremiumUser = watchApiRes.IsPremium;
			DownloadProgress = qualityNicoVideo.Progress;
			DownloadInterval = DownloadDelay;
			CacheFile = cacheFile;
			Size = qualityNicoVideo.VideoSize;

			_DownloadTaskLock = new SemaphoreSlim(1, 1);
			_CacheWriteSemaphore = new SemaphoreSlim(1, 1);

			_ProgressReadWriteLock = new SemaphoreSlim(1, 1);
		}


		public override void Dispose()
		{
			IsClosed = true;

			if (!IsCacheComplete)
			{
				OnCacheCanceled?.Invoke(RawVideoId);
			}

			base.Dispose();
		}


		public IAsyncOperation<IBuffer> Read(IBuffer buffer, uint position, uint count, InputStreamOptions options)
		{
			return AsyncInfo.Run<IBuffer>(async cancellationToken => 
			{
				IBuffer videoFragmentBuffer = buffer;
				// まだキャッシュが終わってない場合は指定区間のダウンロード完了を待つ

				var waitCount = 0;
				while (!CurrentPositionIsCached(position, count))
				{
					cancellationToken.ThrowIfCancellationRequested();

					await Task.Delay(250).ConfigureAwait(false);

					Debug.Write("キャッシュ待ち...");

					waitCount++;

//					if (_DownloadTask == null) { throw new Exception(); }
				}

				if (waitCount != 0)
				{
					await Task.Delay(250);

					cancellationToken.ThrowIfCancellationRequested();
				}

				if (!CurrentPositionIsCached(position, count))
				{
					throw new Exception();
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
								var videoFragmentStream = stream.GetInputStreamAt(position);

								videoFragmentBuffer = await videoFragmentStream.ReadAsync(buffer, count, options).AsTask(cancellationToken);

								Debug.WriteLine($"read: {position} + {videoFragmentBuffer.Length}");

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

				return videoFragmentBuffer;
			});
				
			
		}




		public async Task Download()
		{
			if (IsCacheComplete || IsClosed)
			{
				return;
			}

			await StartDownloadTask(0);
		}

		public async Task StopDownload()
		{
			var stopped = await _StopDownload();
			if (stopped)
			{
//				OnCacheCanceled?.Invoke(RawVideoId);
			}
		}


		public bool IsCacheRequested { get; set; }



		#region Download and Cache writing

		private async Task<bool> _StopDownload()
		{
			try
			{
				await _DownloadTaskLock.WaitAsync();

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


				_DownloadTaskCancelToken?.Cancel();

				//				_ReadAsyncAction?.Cancel();
				//				_ReadAsyncAction = null;


				Debug.Write("ダウンロードキャンセルを待機中");


				await _DownloadTask.WaitToCompelation(count:10);


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

		public async Task WaitToCancel(CancellationToken cancellationToken)
		{
			for (int i = 0; i < 100; i++)
			{
				cancellationToken.ThrowIfCancellationRequested();

				await Task.Delay(250).ConfigureAwait(false);

				Debug.Write("キャンセル待ち...");
			}
		}


		internal async Task StartDownloadTask(uint position)
		{
			try
			{
				await _DownloadTaskLock.WaitAsync();

				if (IsClosed)
				{
					return;
				}

				if (_DownloadTask != null)
				{
					return;
				}


				Debug.WriteLine(RawVideoId + ":" + position + " からダウンロードを開始");

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
			CurrentDownloadHead = offset;
			// TODO: 終了予定時刻の計算?

			// ダウンロードスピードを制限する
			// プレミアム = 約500kbps
			// 一般 = 約250kbps

			if (IsPremiumUser)
			{
				Debug.WriteLine("プレミアムユーザーで" + RawVideoId + "のダウンロードを開始。");
			}
			else
			{
				Debug.WriteLine("一般ユーザーで" + RawVideoId + "のダウンロードを開始。");
			}



			return Task.Run(async () =>
			{
				// シーク後の位置だけをダウンロードする
				var ranges = DownloadProgress.EnumerateIncompleteRanges().ToList();
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
				if (IsCacheRequested && !DownloadProgress.CheckComplete())
				{
					Debug.WriteLine("キャッシュ保存のためダウンロードを継続");

					ranges = DownloadProgress.EnumerateIncompleteRanges().ToList();

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
			Debug.WriteLine($"download: {start} + {size}");

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
			}, retryInterval: 250);

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

			IBuffer resultBuffer = null;
			for (int i = 0; i < 3; i++)
			{
				using (var cancelTokenSource = new CancellationTokenSource(30000))
				{
					resultBuffer = await inputStream.ReadAsync(DownloadBuffer, readSize, InputStreamOptions.None).AsTask(cancelTokenSource.Token);
				}

				if (resultBuffer != null) { break; }
			}

			if (resultBuffer == null)
			{
				throw new Exception();
			}

			await WriteToVideoFile(head, resultBuffer);

			RecordProgress((uint)head, resultBuffer.Length);

			OnCacheProgress?.Invoke(RawVideoId, Quality, (uint)head, resultBuffer.Length);

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
			if (DownloadProgress.CheckComplete())
			{
				Debug.WriteLine($"{RawVideoId} is download done.");

				OnCacheComplete?.Invoke(RawVideoId);

				try
				{
					await _DownloadTaskLock.WaitAsync();
					_DownloadTask = null;
					_DownloadTaskCancelToken?.Dispose();
					_DownloadTaskCancelToken = null;
				}
				finally
				{
					_DownloadTaskLock.Release();
				}
			}
			else
			{
				OnCacheCanceled?.Invoke(RawVideoId);
			}
		}



		#endregion



		#region Progress management



		private void RecordProgress(ulong position, uint count)
		{
			try
			{
				_ProgressReadWriteLock.Wait();

				DownloadProgress.Update((uint)position, count);
			}
			finally
			{
				_ProgressReadWriteLock.Release();
			}
		}



		public bool CurrentPositionIsCached(uint position, uint length)
		{
			try
			{
				_ProgressReadWriteLock.Wait();

				// Progressがあればキャッシュ済み範囲か取得
				// なければキャッシュ済み(true)を返す
				return DownloadProgress?
					.IsCachedRange((uint)position, length)
					?? true;
			}
			finally
			{
				_ProgressReadWriteLock.Release();
			}
			
		}


		public uint GetDownloadProgress()
		{
			var remain = DownloadProgress.RemainSize();
			return (uint)Size - remain;
		}


		public bool IsCacheComplete
		{
			get
			{
				return DownloadProgress?.CheckComplete() ?? false;
			}
		}


		#endregion


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


	}

}