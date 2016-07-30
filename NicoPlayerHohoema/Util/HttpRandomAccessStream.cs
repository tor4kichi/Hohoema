using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
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

		// No public constructor, factory methods instead to handle async tasks.
		protected HttpRandomAccessStream(HttpClient client, Uri uri)
		{
			this._Client = client;
			_RequestedUri = uri;
			_CurrentPosition = 0;
		}

		static public IAsyncOperation<HttpRandomAccessStream> CreateAsync(HttpClient client, Uri uri)
		{
			HttpRandomAccessStream randomStream = new HttpRandomAccessStream(client, uri);

			return AsyncInfo.Run<HttpRandomAccessStream>(async (cancellationToken) =>
			{
				await randomStream.ReadRequestAsync(0).ConfigureAwait(false);
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
						HttpCompletionOption.ResponseHeadersRead);
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
			
		}


		public virtual Windows.Foundation.IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
		{
			return AsyncInfo.Run<IBuffer, uint>(async (cancellationToken, progress) =>
			{
				IInputStream inputStream;
				try
				{
					inputStream = await ReadRequestAsync(_CurrentPosition).ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex);
					throw;
				}

				var result = await inputStream.ReadAsync(buffer, count, options).AsTask(cancellationToken, progress).ConfigureAwait(false);
				
				// Move position forward.
				_CurrentPosition += result.Length;
				Debug.WriteLine("requestedPosition = {0:N0}", _CurrentPosition);

				inputStream.Dispose();

				return result;
			});
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