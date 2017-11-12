using Microsoft.Toolkit.Uwp.Notifications;
using NicoPlayerHohoema.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Metadata;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.UI.Notifications;
using Windows.UI.ViewManagement;

namespace NicoPlayerHohoema.Models
{
    public delegate void VideoCacheRequestedEventHandler(object sender, NicoVideoCacheRequest request);
    public delegate void VideoCacheRequestCanceledEventHandler(object sender, NicoVideoCacheRequest request);
    public delegate void VideoCacheDownloadStartedEventHandler(object sender, NicoVideoCacheProgress progress);
    public delegate void VideoCacheDownloadProgressEventHandler(object sender, NicoVideoCacheProgress progress);
    public delegate void VideoCacheCompletedEventHandler(object sender, NicoVideoCacheInfo cacheInfo);
    public delegate void VideoCacheDownloadCanceledventHandler(object sender, NicoVideoCacheRequest request);



    public struct BackgroundTransferCompletionInfo
    {
        public string Id { get; set; }
        public BackgroundTaskRegistration TaskRegistration { get; set; }
        public BackgroundDownloader Downloader { get; set; }
        public BackgroundTransferCompletionGroup TransferCompletionGroup { get; set; }
    }


    public class VideoDownloadManager 
    {
        // Note: PendingVideosとダウンロードタスクの復元について
        // ダウンロードタスクはアプリではなくOS側で管理されるため、
        // アプリ再開時にダウンロードタスクを取得し直して
        // ダウンロード操作を再ハンドルする必要があります

        // しかし、アプリがアップデートされると
        // このダウンロードタスクをロストすることになるため、
        // ダウンロードタスクの復元は常にアプリ側で管理しなければいけません
        // 最悪の場合、ダウンロードリクエストされた記録が無くなる可能性があります

        // PendingVideosにはダウンロードタスクに登録されたアイテムも含んでいます

        // TryNextCacheRequestedVideoDownloadでPendingVideosからダウンロード中のリクエストを除外して
        // 次にダウンロードすべきアイテムを検索するようにしています


        public HohoemaApp HohoemaApp { get; private set; }
        public VideoCacheManager MediaManager { get; private set; }



        //        private BackgroundTransferCompletionGroup _BTCG = new BackgroundTransferCompletionGroup();

        //        private BackgroundDownloader _BackgroundDownloader;

        private bool IsInitialized = false;

        private static TileContent GetSuccessTileContent(string videoTitle, string videoId)
        {
            var tileTitle = "キャッシュ完了";
            var tileSubject = videoTitle;
            var tileBody = videoId;
            return new TileContent()
            {
                Visual = new TileVisual()
                {
                    TileMedium = new TileBinding()
                    {
                        Content = new TileBindingContentAdaptive()
                        {
                            Children =
                            {
                                new AdaptiveText()
                                {
                                    Text = tileTitle
                                },

                                new AdaptiveText()
                                {
                                    Text = tileSubject,
                                    HintStyle = AdaptiveTextStyle.CaptionSubtle
                                },

                                new AdaptiveText()
                                {
                                    Text = tileBody,
                                    HintStyle = AdaptiveTextStyle.CaptionSubtle
                                }
                            }
                        }
                    },

                    TileWide = new TileBinding()
                    {
                        Content = new TileBindingContentAdaptive()
                        {
                            Children =
                            {
                                new AdaptiveText()
                                {
                                    Text = tileTitle,
                                    HintStyle = AdaptiveTextStyle.Subtitle
                                },

                                new AdaptiveText()
                                {
                                    Text = tileSubject,
                                    HintStyle = AdaptiveTextStyle.CaptionSubtle
                                },

                                new AdaptiveText()
                                {
                                    Text = tileBody,
                                    HintStyle = AdaptiveTextStyle.CaptionSubtle
                                }
                            }
                        }
                    }
                }
            };
        }

        public VideoDownloadManager(HohoemaApp hohoemaApp, VideoCacheManager mediaManager)
        {
            HohoemaApp = hohoemaApp;
            MediaManager = mediaManager;

            // ダウンロード完了をバックグラウンドで処理
            CoreApplication.BackgroundActivated += CoreApplication_BackgroundActivated;
            CoreApplication.Suspending += CoreApplication_Suspending;
        }

        private void CoreApplication_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            RemoveProgressToast();
        }

        private void CoreApplication_BackgroundActivated(object sender, BackgroundActivatedEventArgs e)
        {
            var taskInstance = e.TaskInstance;
            var deferral = taskInstance.GetDeferral();

            var details = taskInstance.TriggerDetails as BackgroundTransferCompletionGroupTriggerDetails;

            if (details == null) { return; }

            IReadOnlyList<DownloadOperation> downloads = details.Downloads;



            var notifier = ToastNotificationManager.CreateToastNotifier();

            foreach (var dl in downloads)
            {
                try
                {
                    if (dl.Progress.BytesReceived != dl.Progress.TotalBytesToReceive)
                    {
                        continue;
                    }

                    if (dl.ResultFile == null)
                    {
                        continue;
                    }

                    var file = dl.ResultFile;

                    // ファイル名の最後方にある[]の中身の文字列を取得
                    // (動画タイトルに[]が含まれる可能性に配慮)
                    var regex = new Regex("(?:(?:sm|so|lv)\\d*)");
                    var match = regex.Match(file.Name);
                    var id = match.Value;

                    // キャッシュファイルからタイトルを抜き出します
                    // ファイルタイトルの決定は 
                    // DividedQualityNicoVideo.VideoFileName プロパティの
                    // 実装に依存します
                    // 想定された形式は以下の形です

                    // タイトル - [sm12345667].mp4
                    // タイトル - [sm12345667].low.mp4

                    var index = file.Name.LastIndexOf(" - [");
                    var title = file.Name.Remove(index);

                    // トーストのレイアウトを作成
                    ToastContent content = new ToastContent()
                    {
                        Launch = "niconico://" + id,

                        Visual = new ToastVisual()
                        {
                            BindingGeneric = new ToastBindingGeneric()
                            {
                                Children =
                            {
                                new AdaptiveText()
                                {
                                    Text = title,
                                },

                                new AdaptiveText()
                                {
                                    Text = "キャッシュ完了",
                                    HintStyle = AdaptiveTextStyle.CaptionSubtle
                                },

                                new AdaptiveText()
                                {
                                    Text = "ここをタップして再生を開始",
                                    HintStyle = AdaptiveTextStyle.CaptionSubtle
                                }
                            },
                                /*
                                AppLogoOverride = new ToastGenericAppLogo()
                                {
                                    Source = "oneAlarm.png"
                                }
                                */
                            }
                        },
                        /*
                        Actions = new ToastActionsCustom()
                        {
                            Buttons =
                            {
                                new ToastButton("check", "check")
                                {
                                    ImageUri = "check.png"
                                },

                                new ToastButton("cancel", "cancel")
                                {
                                    ImageUri = "cancel.png"
                                }
                            }
                        },
                        */
                        /*
                        Audio = new ToastAudio()
                        {
                            Src = new Uri("ms-winsoundevent:Notification.Reminder")
                        }
                        */
                    };

                    // トースト表示を実行
                    ToastNotification notification = new ToastNotification(content.GetXml());
                    notifier.Show(notification);

                }
                catch { }



            }

            deferral.Complete();
        }

        internal async Task Initialize()
        {
            IsInitialized = false;

            // ダウンロードバックグラウンドタスクの情報を復元
            await RestoreBackgroundDownloadTask();

            IsInitialized = true;
        }
        
        
        
      
        

        
    }
}
