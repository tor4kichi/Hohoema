using NicoPlayerHohoema.Models;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Prism.Windows.Navigation;
using System.Threading;
using NicoPlayerHohoema.Services;
using Microsoft.Practices.Unity;
using System.Runtime.InteropServices.WindowsRuntime;
using NicoPlayerHohoema.Models.Cache;

namespace NicoPlayerHohoema.ViewModels
{
    public abstract class HohoemaVideoListingPageViewModelBase<VIDEO_INFO_VM> : HohoemaListingPageViewModelBase<VIDEO_INFO_VM>
		where VIDEO_INFO_VM : VideoInfoControlViewModel
	{
		public HohoemaVideoListingPageViewModelBase(
            PageManager pageManager, 
            bool useDefaultPageTitle = true
            )
			: base(pageManager, useDefaultPageTitle:useDefaultPageTitle)
		{
            var SelectionItemsChanged = SelectedItems.ToCollectionChanged().ToUnit();

#if DEBUG
			SelectedItems.CollectionChangedAsObservable()
				.Subscribe(x =>
				{
					Debug.WriteLine("Selected Count: " + SelectedItems.Count);
				})

            .AddTo(_CompositeDisposable);
#endif


            PlayAllCommand = SelectionItemsChanged
				.Select(_ => SelectedItems.Count > 0)
				.ToReactiveCommand(false)
				.AddTo(_CompositeDisposable);

			PlayAllCommand
				.SubscribeOnUIDispatcher()
				.Subscribe(_ =>
				{

				// TODO: プレイリストに登録
				// プレイリストを空にしてから選択動画を登録

				//				SelectedVideoInfoItems.First()?.PlayCommand.Execute();
			    })
			    .AddTo(_CompositeDisposable);

            /*
			CancelCacheDownloadRequest = SelectionItemsChanged
				.Select(_ => SelectedItems.Count > 0)
				.ToReactiveCommand(false)
				.AddTo(_CompositeDisposable);

			CancelCacheDownloadRequest
				.SubscribeOnUIDispatcher()
				.Subscribe(async _ =>
				{
					var items = EnumerateCacheRequestedVideoItems().ToList();
					var action = AsyncInfo.Run<uint>(async (cancelToken, progress) => 
					{
						uint count = 0;
						foreach (var item in items)
						{
                            foreach (var quality in item.CachedQualityVideos.ToArray())
                            {
                                await HohoemaApp.CacheManager.CancelCacheRequest(item.RawVideoId, quality.Quality);
                            }

                            ++count;
							progress.Report(count);
						}

						ClearSelection();
					});

					await PageManager.StartNoUIWork("キャッシュリクエストをキャンセル中", items.Count, () => action);
				}
			)
			.AddTo(_CompositeDisposable);
            

            // クオリティ指定無しのキャッシュDLリクエスト
            RequestCacheDownload = SelectionItemsChanged
                .Select(_ => SelectedItems.Count > 0 && CanDownload)
				.ToReactiveCommand(false)
				.AddTo(_CompositeDisposable);

			RequestCacheDownload
				.SubscribeOnUIDispatcher()
				.Subscribe(async _ =>
				{
                    foreach (var item in SelectedItems)
					{
                        await HohoemaApp.CacheManager.RequestCache(item.RawVideoId, NicoVideoQuality.Smile_Original);
					}

					ClearSelection();
					await UpdateList();
				})
			.AddTo(_CompositeDisposable);
            */

            /*
			RegistratioMylistCommand = SelectionItemsChanged
				.Select(x => SelectedItems.Count > 0)
				.ToReactiveCommand(false)
				.AddTo(_CompositeDisposable);
			RegistratioMylistCommand
				.SubscribeOnUIDispatcher()
				.Subscribe(async _ =>
				{
                    var targetMylist = await HohoemaApp.ChoiceMylist();

                    if (targetMylist == null) { return; }

                    var items = SelectedItems.ToList();
					var action = AsyncInfo.Run<uint>(async (cancelToken, progress) =>
					{
						uint progressCount = 0;

						Debug.WriteLine($"一括マイリストに追加を開始...");
						int successCount = 0;
						int existCount = 0;
						int failedCount = 0;
						foreach (var video in SelectedItems)
						{
                            var registrationResult = await HohoemaApp.AddMylistItem(targetMylist, video.Label, video.RawVideoId);

                            switch (registrationResult)
                            {
                                case Mntone.Nico2.ContentManageResult.Success: successCount++; break;
                                case Mntone.Nico2.ContentManageResult.Exist: existCount++; break;
                                case Mntone.Nico2.ContentManageResult.Failed: failedCount++; break;
                                default:
                                    break;
                            }

                            Debug.WriteLine($"{video.Label}[{video.RawVideoId}]:{registrationResult.ToString()}");

                            progressCount++;
                            progress.Report(progressCount);
                        }


                        if (targetMylist.Origin == PlaylistOrigin.LoginUser)
                        {
                            var mylistGroup = targetMylist as UserOwnedMylist;
                            await mylistGroup.Refresh();

                            // マイリストに追加に失敗したものを残すように
                            // 登録済みのアイテムを選択アイテムリストから削除
                            foreach (var item in SelectedItems.ToArray())
                            {
                                if (mylistGroup.CheckRegistratedVideoId(item.RawVideoId))
                                {
                                    SelectedItems.Remove(item);
                                }
                            }
                        }

                        // リフレッシュ
                        


						// ユーザーに結果を通知

						var titleText = $"「{targetMylist.Label}」に {successCount}件 の動画を登録しました";
						var resultText = $"";
						if (existCount > 0)
						{
							resultText += $"重複：{existCount} 件";
						}
						if (failedCount > 0)
						{
							resultText += $"\n登録に失敗した {failedCount}件 は選択されたままです";
						}

                        (App.Current as App).Container.Resolve<Services.NotificationService>().ShowInAppNotification(InAppNotificationPayload.CreateReadOnlyNotification(
                            titleText,
                            TimeSpan.FromSeconds(7)
                            ));
						//					ResetList();

						Debug.WriteLine($"一括マイリストに追加を完了---------------");
						ClearSelection();
					});

					await PageManager.StartNoUIWork("マイリストに追加", items.Count, () => action);
				}
			)
            .AddTo(_CompositeDisposable);
            */

            //Playlists = HohoemaApp.Playlist.Playlists.ToReadOnlyReactiveCollection();
        }





        private bool _CanDownload;
        public bool CanDownload
        {
            get { return _CanDownload; }
            set { SetProperty(ref _CanDownload, value); }
        }

        public ReadOnlyReactiveCollection<LegacyLocalMylist> Playlists { get; private set; }

        public ReactiveCommand PlayAllCommand { get; private set; }
        public ReactiveCommand CancelCacheDownloadRequest { get; private set; }
        public ReactiveCommand DeleteOriginalQualityCache { get; private set; }
        public ReactiveCommand DeleteLowQualityCache { get; private set; }

        public ReactiveCommand RegistratioMylistCommand { get; private set; }



        // クオリティ指定なしのコマンド
        // VMがクオリティを実装している場合には、そのクオリティを仕様
        // そうでない場合は、リクエスト時は低クオリティのみを
        // 削除時はすべてのクオリティの動画を指定してアクションを実行します。
        // 基本的にキャッシュ管理画面でしか使わないはずです
        //public ReactiveCommand RequestCacheDownload { get; private set; }
        //public ReactiveCommand DeleteCache { get; private set; }

		private IEnumerable<VideoInfoControlViewModel> EnumerateCacheRequestedVideoItems()
		{
			return SelectedItems.Where(x =>
			{
                return x.CachedQualityVideos.Count > 0;
            });
		}





	}


    
}
