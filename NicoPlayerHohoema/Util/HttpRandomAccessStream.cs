using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.Web.Http;

namespace NicoPlayerHohoema.Util
{
	public class HttpRandomAccessStream : IRandomAccessStream
	{
		private HttpClient _Client;
		private ulong _Size;
		protected ulong _CurrentPosition;
		private string _EtagHeader;
		private string _LastModifiedHeader;
		private Uri _RequestedUri;

		SemaphoreSlim _DownloadTaskAccessLock = new SemaphoreSlim(1, 1);
		Windows.Foundation.IAsyncOperationWithProgress<IBuffer, uint> _DownloadTask;

		AsyncLock _StreamAccessLock = new AsyncLock();
		IInputStream _InputStream;
		
		// No public constructor, factory methods instead to handle async tasks.
		protected HttpRandomAccessStream(HttpClient client, Uri uri, ulong size)
		{
			this._Client = client;
			_RequestedUri = uri;
			_Size = size;
			_CurrentPosition = 0;
		}

		static public IAsyncOperation<HttpRandomAccessStream> CreateAsync(HttpClient client, Uri uri, ulong size = 0)
		{
			HttpRandomAccessStream randomStream = new HttpRandomAccessStream(client, uri, size);
			return AsyncInfo.Run<HttpRandomAccessStream>(async (cancellationToken) =>
			{
				if (randomStream.Size == 0)
				{
					randomStream._InputStream = await randomStream.ReadRequestAsync(0);

					await Task.Delay(500);
				}

				return randomStream;
			});
		}

		protected async Task<IInputStream> ReadRequestAsync(ulong position)
		{
			var response = await Util.ConnectionRetryUtil.TaskWithRetry(async () =>
			{
				try
				{
					HttpRequestMessage request = null;
					request = new HttpRequestMessage(HttpMethod.Get, _RequestedUri);

					request.Headers.Add("Range", $"bytes={position}-");

					if (!String.IsNullOrEmpty(_EtagHeader))
					{
						request.Headers.Add("If-Match", _EtagHeader);
					}

					if (!String.IsNullOrEmpty(_LastModifiedHeader))
					{
						request.Headers.Add("If-Unmodified-Since", _LastModifiedHeader);
					}

					return await _Client.SendRequestAsync(
						request,
						HttpCompletionOption.ResponseHeadersRead
						)
						.AsTask();
				}
				catch (System.Exception e) when (e.Message.StartsWith("Http server does not support range requests"))
				{
					throw new System.Net.WebException(" failed video content donwload. position:" + position, e);
				}
				catch (System.Runtime.InteropServices.COMException e)
				{
					throw new System.Net.WebException(e.Message, System.Net.WebExceptionStatus.Timeout);
				}
			}, retryInterval:3000);

			Debug.WriteLine(response);


			if (response.StatusCode != HttpStatusCode.PartialContent && position != 0)
			{
				throw new Exception("HTTP server did not reply with a '206 Partial Content' status.");
			}

			_Size = response.Content.Headers.ContentLength.Value;

			if (!response.Headers.ContainsKey("Accept-Ranges"))
			{
				throw new Exception(String.Format(
					"HTTP server does not support range requests: {0}",
					"http://www.w3.org/Protocols/rfc2616/rfc2616-sec14.html#sec14.5"));
			}

			if (String.IsNullOrEmpty(_EtagHeader) && response.Headers.ContainsKey("ETag"))
			{
				_EtagHeader = response.Headers["ETag"];
			}

			if (String.IsNullOrEmpty(_LastModifiedHeader) && response.Content.Headers.ContainsKey("Last-Modified"))
			{
				_LastModifiedHeader = response.Content.Headers["Last-Modified"];
			}
			if (response.Content.Headers.ContainsKey("Content-Type"))
			{
				contentType = response.Content.Headers["Content-Type"];
			}

			return await response.Content.ReadAsInputStreamAsync().AsTask().ConfigureAwait(false);
		}

		private string contentType = string.Empty;

		public string ContentType
		{
			get { return contentType; }
			private set { contentType = value; }
		}

		public bool CanRead
		{
			get
			{
				return true;
			}
		}

		public bool CanWrite
		{
			get
			{
				return false;
			}
		}

		public IRandomAccessStream CloneStream()
		{
			throw new NotImplementedException();
		}

		public IInputStream GetInputStreamAt(ulong position)
		{
			throw new NotImplementedException();
		}

		public IOutputStream GetOutputStreamAt(ulong position)
		{
			throw new NotImplementedException();
		}

		public ulong Position
		{
			get
			{
				return _CurrentPosition;
			}
		}

		public virtual void Seek(ulong position)
		{
			if (_CurrentPosition != position)
			{
				Debug.WriteLine($"Seek: {_CurrentPosition:N0} -> {position:N0}");
				_CurrentPosition = position;

				ResetInputStream().ConfigureAwait(false);
			}
		}

		public ulong Size
		{
			get
			{
				return _Size;
			}
			set
			{
				_Size = value;
			}
		}

		public virtual async void Dispose()
		{
			try
			{
				_DownloadTaskAccessLock.Wait();

				if (_DownloadTask != null)
				{
					_DownloadTask.Cancel();

					await _DownloadTask;
				}
			}
			finally
			{
				_DownloadTaskAccessLock.Release();
			}

			using (var releaser = await _StreamAccessLock.LockAsync())
			{
				_InputStream?.Dispose();
				_InputStream = null;
			}
		}


		public virtual Windows.Foundation.IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
		{
			try
			{
				_DownloadTaskAccessLock.Wait();

				var task = AsyncInfo.Run<IBuffer, uint>(async (cancellationToken, progress) =>
				{
					using (var releaser = await _StreamAccessLock.LockAsync().ConfigureAwait(false))
					{
						cancellationToken.ThrowIfCancellationRequested();

						if (_InputStream == null)
						{
							_InputStream = await ReadRequestAsync(_CurrentPosition).ConfigureAwait(false);
						}

						var result = await _InputStream.ReadAsync(buffer, count, options).AsTask(cancellationToken, progress).ConfigureAwait(false);

						cancellationToken.ThrowIfCancellationRequested();

						Debug.WriteLine($"read:[{_CurrentPosition}] - [{_CurrentPosition + result.Length}]");

						// Move position forward.
						_CurrentPosition += result.Length;

						return result;
					}
				});
				_DownloadTask = task;

				return task;
			}
			finally
			{
				_DownloadTaskAccessLock.Release();
			}				
		}

		
		private async Task ResetInputStream()
		{
			using (var releaser = await _StreamAccessLock.LockAsync())
			{				
				_InputStream?.Dispose();
				_InputStream = null;
			}
		}
		

	
		public Windows.Foundation.IAsyncOperation<bool> FlushAsync()
		{
			throw new NotImplementedException();
		}

		public Windows.Foundation.IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer)
		{
			throw new NotImplementedException();
		}
	}



	public class HttpSequencialAccessStream : IRandomAccessStream
	{
		private HttpClient _Client;
		private ulong _Size;
		protected ulong _CurrentPosition;
		private string _EtagHeader;
		private string _LastModifiedHeader;
		private Uri _RequestedUri;

		SemaphoreSlim _DownloadTaskAccessLock = new SemaphoreSlim(1, 1);

		SemaphoreSlim _StreamAccessLock = new SemaphoreSlim(1, 1);
		IInputStream _InputStream;

		// No public constructor, factory methods instead to handle async tasks.
		protected HttpSequencialAccessStream(HttpClient client, Uri uri)
		{
			this._Client = client;
			_RequestedUri = uri;
			_Size = 0;
			_CurrentPosition = 0;
		}

		static public IAsyncOperation<HttpSequencialAccessStream> CreateAsync(HttpClient client, Uri uri)
		{
			HttpSequencialAccessStream randomStream = new HttpSequencialAccessStream(client, uri);
			return AsyncInfo.Run<HttpSequencialAccessStream>(async (cancellationToken) =>
			{
				randomStream._InputStream = await randomStream.ReadRequestAsync(0);

				await Task.Delay(500);

				return randomStream;
			});
		}

		protected async Task<IInputStream> ReadRequestAsync(ulong position)
		{
			var response = await Util.ConnectionRetryUtil.TaskWithRetry(async () =>
			{
				try
				{
					HttpRequestMessage request = null;
					request = new HttpRequestMessage(HttpMethod.Get, _RequestedUri);

					request.Headers.Add("Range", $"bytes={position}-");

					if (!String.IsNullOrEmpty(_EtagHeader))
					{
						request.Headers.Add("If-Match", _EtagHeader);
					}

					if (!String.IsNullOrEmpty(_LastModifiedHeader))
					{
						request.Headers.Add("If-Unmodified-Since", _LastModifiedHeader);
					}

					return await _Client.SendRequestAsync(
						request,
						HttpCompletionOption.ResponseHeadersRead
						)
						.AsTask()
						.ConfigureAwait(false);
				}
				catch (System.Exception e) when (e.Message.StartsWith("Http server does not support range requests"))
				{
					throw new System.Net.WebException(" failed video content donwload. position:" + position, e);
				}
				catch (System.Runtime.InteropServices.COMException e)
				{
					throw new System.Net.WebException(e.Message, System.Net.WebExceptionStatus.Timeout);
				}
			}, retryInterval: 3000)
			.ConfigureAwait(false);

			Debug.WriteLine(response);


			if (response.StatusCode != HttpStatusCode.PartialContent && position != 0)
			{
				throw new Exception("HTTP server did not reply with a '206 Partial Content' status.");
			}

			_Size = response.Content.Headers.ContentLength.Value;

			if (!response.Headers.ContainsKey("Accept-Ranges"))
			{
				throw new Exception(String.Format(
					"HTTP server does not support range requests: {0}",
					"http://www.w3.org/Protocols/rfc2616/rfc2616-sec14.html#sec14.5"));
			}

			if (String.IsNullOrEmpty(_EtagHeader) && response.Headers.ContainsKey("ETag"))
			{
				_EtagHeader = response.Headers["ETag"];
			}

			if (String.IsNullOrEmpty(_LastModifiedHeader) && response.Content.Headers.ContainsKey("Last-Modified"))
			{
				_LastModifiedHeader = response.Content.Headers["Last-Modified"];
			}
			if (response.Content.Headers.ContainsKey("Content-Type"))
			{
				contentType = response.Content.Headers["Content-Type"];
			}

			return await response.Content.ReadAsInputStreamAsync().AsTask().ConfigureAwait(false);
		}

		private string contentType = string.Empty;

		public string ContentType
		{
			get { return contentType; }
			private set { contentType = value; }
		}

		public bool CanRead
		{
			get
			{
				return true;
			}
		}

		public bool CanWrite
		{
			get
			{
				return false;
			}
		}

		public IRandomAccessStream CloneStream()
		{
			throw new NotImplementedException();
		}

		public IInputStream GetInputStreamAt(ulong position)
		{
			throw new NotImplementedException();
		}

		public IOutputStream GetOutputStreamAt(ulong position)
		{
			throw new NotImplementedException();
		}

		public ulong Position
		{
			get
			{
				return _CurrentPosition;
			}
		}

		public virtual void Seek(ulong position)
		{
			if (_CurrentPosition != position)
			{
				Debug.WriteLine($"Seek: {_CurrentPosition:N0} -> {position:N0}");
				_CurrentPosition = position;

				ResetInputStream().ConfigureAwait(false);
			}
		}

		public ulong Size
		{
			get
			{
				return _Size;
			}
			set
			{
				_Size = value;
			}
		}

		public virtual async void Dispose()
		{			
			try
			{
				await _StreamAccessLock.WaitAsync();

				_InputStream?.Dispose();
				_InputStream = null;
			}
			finally
			{
				_StreamAccessLock.Release();
			}
		}


		public virtual Windows.Foundation.IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
		{
			try
			{
				_StreamAccessLock.Wait();
				_CurrentPosition += count;
				return _InputStream.ReadAsync(buffer, count, options);
			}
			finally
			{
				_StreamAccessLock.Release();
			}
		}


		private async Task ResetInputStream()
		{
			try
			{
				await _StreamAccessLock.WaitAsync();

				var inputStream = _InputStream;
				_InputStream = await ReadRequestAsync(_CurrentPosition).ConfigureAwait(false);
				inputStream?.Dispose();
			}
			finally
			{
				_StreamAccessLock.Release();
			}
		}



		public Windows.Foundation.IAsyncOperation<bool> FlushAsync()
		{
			throw new NotImplementedException();
		}

		public Windows.Foundation.IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer)
		{
			throw new NotImplementedException();
		}
	}

}