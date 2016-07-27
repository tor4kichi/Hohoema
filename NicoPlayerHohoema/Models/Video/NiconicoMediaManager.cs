using Mntone.Nico2.Videos.Thumbnail;
using Mntone.Nico2.Videos.WatchAPI;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;

namespace NicoPlayerHohoema.Models
{
	/// <summary>
	/// ニコニコ動画の動画やサムネイル画像、
	/// 動画情報など動画に関わるメディアを管理します
	/// </summary>
	public class NiconicoMediaManager : BindableBase, IDisposable
	{
		static internal async Task<NiconicoMediaManager> Create(HohoemaApp app)
		{
			var man = new NiconicoMediaManager(app);
			man.Context = await NicoVideoDownloadContext.Create(app);
			
			// ダウンロードリクエストされたアイテムのNicoVideoオブジェクトの作成
			// 及び、リクエストの再構築
			var list = await man.Context.LoadDownloadRequestItems();
			foreach (var req in list)
			{
				var nicoVideo = await man.GetNicoVideo(req.RawVideoid);
				await nicoVideo.RequestCache(req.Quality);
			}

			Debug.WriteLine($"{list.Count}件のダウンロード待ち状況を復元しました。");

			// キャッシュ済みアイテムのNicoVideoオブジェクトの作成
			var saveFolder = man.Context.VideoSaveFolder;
			var files = await saveFolder.GetFilesAsync();

			var cachedFiles = files
				.Where(x => x.Name.EndsWith("_info.json"))
				.Select(x => new String(x.Name.TakeWhile(y => y != '_').ToArray()));

			foreach (var cachedFile in cachedFiles)
			{
				await man.GetNicoVideo(cachedFile);
			}

			var deletedCachedFiles = files
				.Where(x => x.Name.EndsWith("_info.json" + NicoVideo.DELETED_EXT))
				.Select(x => new String(x.Name.TakeWhile(y => y != '_').ToArray()));

			foreach (var deletedCachedFile in deletedCachedFiles)
			{
				await man.SetupDeletedVideo(deletedCachedFile);
			}


			return man;
		}

		

		private NiconicoMediaManager(HohoemaApp app)
		{
			_HohoemaApp = app;

			VideoIdToNicoVideo = new Dictionary<string, NicoVideo>();

			_NicoVideoSemaphore = new SemaphoreSlim(1, 1);

		}


		public async Task<NicoVideo> SetupDeletedVideo(string rawVideoId)
		{
			try
			{
				await _NicoVideoSemaphore.WaitAsync();

				if (VideoIdToNicoVideo.ContainsKey(rawVideoId))
				{
					return VideoIdToNicoVideo[rawVideoId];
				}
				else
				{
					var nicoVideo = await NicoVideo.CreateWithDeleted(_HohoemaApp, rawVideoId, Context);
					VideoIdToNicoVideo.Add(rawVideoId, nicoVideo);
					return nicoVideo;
				}
			}
			finally
			{
				_NicoVideoSemaphore.Release();
			}
		}


		public async Task<NicoVideo> GetNicoVideo(string rawVideoId)
		{
			try
			{
				await _NicoVideoSemaphore.WaitAsync();

				if (VideoIdToNicoVideo.ContainsKey(rawVideoId))
				{
					return VideoIdToNicoVideo[rawVideoId];
				}
				else
				{
					var nicoVideo = await NicoVideo.Create(_HohoemaApp, rawVideoId, Context);
					VideoIdToNicoVideo.Add(rawVideoId, nicoVideo);
					return nicoVideo;
				}
			}
			finally
			{
				_NicoVideoSemaphore.Release();
			}
		}



		public void Dispose()
		{
			Context.Dispose();
		}


		private SemaphoreSlim _NicoVideoSemaphore;

		public Dictionary<string, NicoVideo> VideoIdToNicoVideo { get; private set; }

		public NicoVideoDownloadContext Context { get; private set; }
		HohoemaApp _HohoemaApp;
	}





	


}
