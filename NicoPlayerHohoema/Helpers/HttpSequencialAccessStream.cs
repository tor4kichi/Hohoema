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

namespace NicoPlayerHohoema.Helpers
{
	public class HttpSequencialAccessStream : IRandomAccessStreamWithContentType
	{
		private HttpClient _Client;
		private ulong _Size;
		protected ulong _CurrentPosition;
		private string _EtagHeader;
		private string _LastModifiedHeader;
		private Uri _RequestedUri;

		SemaphoreSlim _DownloadTaskAccessLock = new SemaphoreSlim(1, 1);

		public SemaphoreSlim StreamAccessLock { get; } = new SemaphoreSlim(1, 1);
		IInputStream _InputStream;
        bool _IsDisposed = false;

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
			HttpSequencialAccessStream stream = new HttpSequencialAccessStream(client, uri);
			return AsyncInfo.Run<HttpSequencialAccessStream>(async (cancellationToken) =>
			{
				stream._InputStream = await stream.ReadRequestAsync(0);

				await Task.Delay(100);

				return stream;
			});
		}

		protected async Task<IInputStream> ReadRequestAsync(ulong position)
		{
			var response = await Helpers.ConnectionRetryUtil.TaskWithRetry(async () =>
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

			return await response.Content.ReadAsInputStreamAsync().AsTask();
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
            return this;
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
            if (_IsDisposed) { return; }

			if (_CurrentPosition != position)
			{
				Debug.WriteLine($"Seek: {_CurrentPosition:N0} -> {position:N0}");
				_CurrentPosition = position;

                var task = ResetInputStream();
                task.Wait();
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

		public virtual void Dispose()
		{			
			try
			{
				StreamAccessLock.Wait();

                _IsDisposed = true;

                _InputStream?.Dispose();
				_InputStream = null;
			}
			finally
			{
				StreamAccessLock.Release();
			}
		}


		public virtual Windows.Foundation.IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
		{
			try
			{
                StreamAccessLock.Wait();
                if (_IsDisposed)
                {
                    return AsyncInfo.Run<IBuffer, uint>((token, progress) =>
                    {
                        return Task.FromResult(buffer);
                    });
                }
				_CurrentPosition += count;
                return _InputStream.ReadAsync(buffer, count, options);
			}
			finally
			{
				StreamAccessLock.Release();
			}
		}


		private async Task ResetInputStream()
		{
			try
			{
				await StreamAccessLock.WaitAsync();

                if (_IsDisposed) { return; }

				var inputStream = _InputStream;
				_InputStream = await ReadRequestAsync(_CurrentPosition);
				inputStream?.Dispose();
            }
			finally
			{
				StreamAccessLock.Release();
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