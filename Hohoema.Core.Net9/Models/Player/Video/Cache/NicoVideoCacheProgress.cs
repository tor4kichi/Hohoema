﻿#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Hohoema.Contracts.Services;
using Hohoema.Helpers;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Networking.BackgroundTransfer;
using Windows.UI.Notifications;

namespace Hohoema.Models.Player.Video.Cache;

public class NicoVideoCacheProgress : ObservableObject, IDisposable
{
    public string VideoId { get; set; }
    public string VideoTitle { get; set; }
    public NicoVideoQuality_Legacy Quality { get; set; }
    public DateTime RequestAt { get; set; } = DateTime.Now;

    public DownloadOperation DownloadOperation { get; set; }
    public IStreamingSession Session { get; }


    public NicoVideoCacheProgress(DownloadOperation op, IVideoStreamingDownloadSession session, string videoId, string videoTitle, NicoVideoQuality_Legacy quality, DateTime requestAt)
    {
        DownloadOperation = op;
        Session = session;
        VideoId = videoId;
        VideoTitle = videoTitle;
        Quality = quality;
        RequestAt = requestAt;


        SendUpdatableToastWithProgress();

        Progress = 0.0;
        _invertedProgressTotal = 1.0 / op.Progress.TotalBytesToReceive;
    }



    public async void AttachAsync()
    {
        IAsyncOperationWithProgress<DownloadOperation, DownloadOperation> progress = DownloadOperation.AttachAsync();
        progress.Progress = (_, op) => OnProgress(op);
        await progress
            .AsTask()
            .ContinueWith(OnDownloadCompleted);

        try
        {
            DownloadOperation.Resume();
        }
        catch
        {
            if (DownloadOperation.Progress.Status != BackgroundTransferStatus.Running)
            {
                Failed?.Invoke(this, EventArgs.Empty);
            }
        }
    }


    public async void StartAsync()
    {
        IAsyncOperationWithProgress<DownloadOperation, DownloadOperation> progress = DownloadOperation.StartAsync();
        progress.Progress = (_, op) => OnProgress(op);
        await progress
            .AsTask()
            .ContinueWith(OnDownloadCompleted);

#if DEBUG
        Debug.WriteLine("DL Operation Headers.");
        ResponseInformation info = DownloadOperation.GetResponseInformation();
        foreach (System.Collections.Generic.KeyValuePair<string, string> header in info.Headers)
        {
            Debug.WriteLine(header.Key + ":" + header.Value);
        }
        Debug.WriteLine("DL Operation Headers end.");
#endif

    }

    public event TypedEventHandler<NicoVideoCacheProgress, EventArgs> Failed;
    public event TypedEventHandler<NicoVideoCacheProgress, EventArgs> Completed;
    public event TypedEventHandler<NicoVideoCacheProgress, EventArgs> Canceled;


    void IDisposable.Dispose()
    {
        RemoveProgressToast();
        Session?.Dispose();
    }


    public async Task CancelAndDeleteFileAsync()
    {
        DownloadOperation op = DownloadOperation;
        using CancellationTokenSource canceledToken = new();
        canceledToken.Cancel();

        bool isNotCompleted = op.Progress.BytesReceived != op.Progress.TotalBytesToReceive;
        try
        {
            _ = await op.AttachAsync().AsTask(canceledToken.Token);
        }
        catch (TaskCanceledException)
        {

        }

        if (isNotCompleted)
        {
            await op.ResultFile.DeleteAsync();
        }
    }


    // ダウンロード完了
    private void OnDownloadCompleted(Task<DownloadOperation> prevTask)
    {
        if (prevTask.IsFaulted)
        {
            Debug.WriteLine("キャッシュ失敗");
            Failed?.Invoke(this, EventArgs.Empty);
            return;
        }

        Debug.WriteLine("キャッシュ完了");

        if (prevTask.Result != null)
        {
            DownloadOperation op = DownloadOperation;
            if (op.Progress.Status == BackgroundTransferStatus.Completed)
            {
                if (op.Progress.TotalBytesToReceive == op.Progress.BytesReceived)
                {
                    Debug.WriteLine("キャッシュ済み: " + op.ResultFile.Name);
                    Completed?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    Debug.WriteLine("キャッシュキャンセル: " + op.ResultFile.Name);
                    Canceled?.Invoke(this, EventArgs.Empty);
                }
            }
            else
            {
                Debug.WriteLine($"キャッシュ失敗: {op.ResultFile.Name} ");
                Failed?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private readonly double _invertedProgressTotal;

    private double _progress = 0.0;
    public double Progress
    {
        get => _progress;
        private set => SetProperty(ref _progress, value);
    }

    private void OnProgress(DownloadOperation op)
    {
        Debug.WriteLine($"{op.RequestedUri}:{op.Progress.TotalBytesToReceive}");

        if (!double.IsInfinity(_invertedProgressTotal))
        {
            Progress = op.Progress.BytesReceived * _invertedProgressTotal;
        }

        UpdateProgressToast(VideoId, op);
    }

    /*
     *https://blogs.msdn.microsoft.com/tiles_and_toasts/2017/02/01/progress-ui-and-data-binding-inside-toast-notifications-windows-10-creators-update/
     */
    private ToastNotification _ProgressToast;
    private void SendUpdatableToastWithProgress()
    {
        if (!ApiContractHelper.IsCreatorsUpdateAvailable)
        {
            return;
        }

        // Define a tag value and a group value to uniquely identify a notification, in order to target it to apply the update later;
        string toastTag = $"{VideoId}";
        string toastGroup = "hohoema_cache_dl";

        ILocalizeService localizeService = Ioc.Default.GetRequiredService<ILocalizeService>();

        // Construct the toast content with updatable data fields inside;
        ToastContent content = new()
        {
            Visual = new ToastVisual()
            {
                BindingGeneric = new ToastBindingGeneric()
                {
                    Children =
            {
                new AdaptiveText()
                {
                    Text = VideoTitle,
                    HintStyle = AdaptiveTextStyle.Header
                },

                new AdaptiveProgressBar()
                {
                    Value = new BindableProgressBarValue("progressValue"),
                    ValueStringOverride = new BindableString("progressString"),
                    Status = new BindableString("progressStatus")
                }
            }
                }
            },
            Actions = new ToastActionsCustom()
            {
                Buttons =
                {
                    new ToastButton(localizeService.Translate("Cancel"), $"cache_cancel?id={VideoId}")
                    {

                    }
                }
            },
        };

        // Generate the toast notification;
        ToastNotification toast = new(content.GetXml())
        {
            // Assign the tag and group properties;
            Tag = toastTag,
            Group = toastGroup,

            // Define NotificationData property and add it to the toast notification to bind the initial data;
            // Data.Values are assigned with string values;
            Data = new NotificationData()
        };
        toast.Data.Values["progressValue"] = "0";
        toast.Data.Values["progressString"] = $"";
        toast.Data.Values["progressStatus"] = "download started";

        // Provide sequence number to prevent updating out-of-order or assign it with value 0 to indicate "always update";
        toast.Data.SequenceNumber = 1;

        toast.SuppressPopup = true;

        // Show the toast notification to the user;
        ToastNotificationManager.CreateToastNotifier().Show(toast);

        _ProgressToast = toast;
    }

    private void UpdateProgressToast(string videoId, DownloadOperation op)
    {
        if (!ApiContractHelper.IsCreatorsUpdateAvailable)
        {
            return;
        }

        // Construct a NotificationData object;
        string toastTag = $"{videoId}";
        string toastGroup = "hohoema_cache_dl";

        double progress = op.Progress.BytesReceived / (double)op.Progress.TotalBytesToReceive;
        string progressText = (progress * 100).ToString("F0");
        // Create NotificationData with new values;
        // Make sure that sequence number is incremented since last update, or assign with value 0 for updating regardless of order;
        NotificationData data = new() { SequenceNumber = 0 };

        data.Values["progressValue"] = progress.ToString("F1"); // 固定小数点、整数部と小数一桁までを表示
        data.Values["progressString"] = $"{progressText}%";
        data.Values["progressStatus"] = "donwloading";

        // Updating a previously sent toast with tag, group, and new data;
        _ = ToastNotificationManager.CreateToastNotifier().Update(data, toastTag, toastGroup);
    }

    private void RemoveProgressToast()
    {
        if (!ApiContractHelper.IsCreatorsUpdateAvailable)
        {
            return;
        }

        // Construct a NotificationData object;
        //            string toastTag = $"{req.RawVideoId}_{req.Quality}";
        //            string toastGroup = "hohoema_cache_dl";

        // Updating a previously sent toast with tag, group, and new data;
        if (_ProgressToast != null)
        {
            ToastNotificationManager.CreateToastNotifier().Hide(_ProgressToast);
            _ProgressToast = null;
        }
    }


}
