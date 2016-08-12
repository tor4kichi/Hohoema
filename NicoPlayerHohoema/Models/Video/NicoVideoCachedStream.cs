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
	public class NicoVideoCachedStream : IRandomAccessStream
	{
		public NicoVideoDownloader Downloader;


		private SemaphoreSlim _ReaderAssignLock;
		public bool IsClosed { get; private set; }
		public NicoVideoCachedStream(NicoVideoDownloader downloader)
		{
			Downloader = downloader;
			_ReaderAssignLock = new SemaphoreSlim(1, 1);
			_SeekLock = new SemaphoreSlim(1, 1);
		}

		private ulong _CurrentPosition;


		private SemaphoreSlim _SeekLock;


		#region override HttpRandomAccessStream

		public async void Seek(ulong position)
		{
			try
			{
				await _SeekLock.WaitAsync();

				if (_CurrentPosition != position)
				{
					Debug.WriteLine($"Seek: {_CurrentPosition:N0} -> {position:N0}");
					_CurrentPosition = position;

					if (Downloader.CurrentDownloadHead != position)
					{
						try
						{
							await Downloader.StopDownload();

							if (IsClosed)
							{
								return;
							}

							await Downloader.StartDownloadTask((uint)position);
						}
						catch (Exception ex)
						{
							Debug.WriteLine(ex.Message);
						}
					}
					else
					{
						Debug.WriteLine("seeking but CurrentDownloadHead is not changed.");
					}
				}
			}
			finally
			{
				_SeekLock.Release();
			}
		}

		IAsyncOperationWithProgress<IBuffer, uint> _ReadAsyncOperation;
		public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
		{
			var action = AsyncInfo.Run<IBuffer, uint>(async (cancellationToken, progress) =>
			{
				cancellationToken.ThrowIfCancellationRequested();


				IBuffer videoFragmentBuffer = null;

				try
				{
					videoFragmentBuffer = await Downloader.Read(buffer, (uint)_CurrentPosition, count, options).AsTask(cancellationToken);
					_CurrentPosition += videoFragmentBuffer.Length;
				}
				catch
				{
					// Downloader.waitClose
					await Downloader.WaitToCancel(cancellationToken);
				}

				cancellationToken.ThrowIfCancellationRequested();

				return videoFragmentBuffer;
			});

			try
			{
				_ReaderAssignLock.Wait();
				if (IsClosed)
				{
					action?.Cancel();
				}
				else
				{
					_ReadAsyncOperation = action;
				}
			}
			finally
			{
				_ReaderAssignLock.Release();
			}
			
			return action;
		}

		public IInputStream GetInputStreamAt(ulong position)
		{
			throw new NotImplementedException();
		}

		public IOutputStream GetOutputStreamAt(ulong position)
		{
			throw new NotImplementedException();
		}

		public IRandomAccessStream CloneStream()
		{
			throw new NotImplementedException();
		}

		public IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer)
		{
			throw new NotImplementedException();
		}

		public IAsyncOperation<bool> FlushAsync()
		{
			throw new NotImplementedException();
		}

		public void Dispose()
		{
			IsClosed = true;

			try
			{
				_ReaderAssignLock.Wait();

				_ReadAsyncOperation?.Cancel();
			}
			finally
			{
				_ReaderAssignLock.Release();
			}

		}


		#endregion



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

		public ulong Position
		{
			get
			{
				return _CurrentPosition;
			}
		}

		public ulong Size
		{
			get
			{
				return Downloader.Size;
			}

			set
			{
				throw new NotImplementedException();
			}
		}
	}

	
	
}
