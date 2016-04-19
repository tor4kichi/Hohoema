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

	public struct BufferMappingInfo
	{
		public BufferMappingInfo(ulong originalPos, ulong memoryPos, uint size, ulong registerTiming)
		{
			RegisterTiming = registerTiming;
			OriginalPosition = originalPos;
			MemoryPosition = memoryPos;
			Size = size;
		}

		public ulong RegisterTiming { get; private set; }
		public ulong OriginalPosition { get; private set; }

		public ulong MemoryPosition { get; private set; }
		public uint Size { get; private set; }
	}

	public class HttpRandomAccessStream : IRandomAccessStream
	{
		private HttpClient client;
		private ulong size;
		private ulong currentPosition;
		private string etagHeader;
		private string lastModifiedHeader;
		private Uri requestedUri;

		private InMemoryRandomAccessStream BufferdMemory;
		private Dictionary<ulong, BufferMappingInfo> PositionToBufferInfo;

		private ulong ReadRequestCount;

		// No public constructor, factory methods instead to handle async tasks.
		private HttpRandomAccessStream(HttpClient client, Uri uri, uint memoryBufferSize = 1048576)
		{
			this.client = client;
			requestedUri = uri;
			currentPosition = 0;
		}

		static public IAsyncOperation<HttpRandomAccessStream> CreateAsync(HttpClient client, Uri uri)
		{
			HttpRandomAccessStream randomStream = new HttpRandomAccessStream(client, uri);

			return AsyncInfo.Run<HttpRandomAccessStream>(async (cancellationToken) =>
			{
				await randomStream.ReadRequestAsync().ConfigureAwait(false);
				return randomStream;
			});
		}

		private async Task<IInputStream> ReadRequestAsync()
		{
			HttpRequestMessage request = null;
			request = new HttpRequestMessage(HttpMethod.Get, requestedUri);

			request.Headers.Add("Range", $"bytes={currentPosition}-");

			if (!String.IsNullOrEmpty(etagHeader))
			{
				request.Headers.Add("If-Match", etagHeader);
			}

			if (!String.IsNullOrEmpty(lastModifiedHeader))
			{
				request.Headers.Add("If-Unmodified-Since", lastModifiedHeader);
			}

			HttpResponseMessage response = await client.SendRequestAsync(
				request,
				HttpCompletionOption.ResponseHeadersRead).AsTask().ConfigureAwait(false);

//			Debug.WriteLine(response);

			size = response.Content.Headers.ContentLength.Value;

			if (response.StatusCode != HttpStatusCode.PartialContent && currentPosition != 0)
			{
				throw new Exception("HTTP server did not reply with a '206 Partial Content' status.");
			}

			if (!response.Headers.ContainsKey("Accept-Ranges"))
			{
				throw new Exception(String.Format(
					"HTTP server does not support range requests: {0}",
					"http://www.w3.org/Protocols/rfc2616/rfc2616-sec14.html#sec14.5"));
			}

			if (String.IsNullOrEmpty(etagHeader) && response.Headers.ContainsKey("ETag"))
			{
				etagHeader = response.Headers["ETag"];
			}

			if (String.IsNullOrEmpty(lastModifiedHeader) && response.Content.Headers.ContainsKey("Last-Modified"))
			{
				lastModifiedHeader = response.Content.Headers["Last-Modified"];
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
				return currentPosition;
			}
		}

		public void Seek(ulong position)
		{
			if (currentPosition != position)
			{
				Debug.WriteLine($"Seek: {currentPosition:N0} -> {position:N0}");
				currentPosition = position;
			}
		}

		public ulong Size
		{
			get
			{
				return size;
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		public virtual void Dispose()
		{
			BufferdMemory?.Dispose();
		}


		public Windows.Foundation.IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
		{
			return AsyncInfo.Run<IBuffer, uint>(async (cancellationToken, progress) =>
			{
				IInputStream inputStream;
				try
				{
					inputStream = await ReadRequestAsync().ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex);
					throw;
				}

				var result = await inputStream.ReadAsync(buffer, count, options).AsTask(cancellationToken, progress).ConfigureAwait(false);
				
				// Move position forward.
				currentPosition += result.Length;
				Debug.WriteLine("requestedPosition = {0:N0}", currentPosition);
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