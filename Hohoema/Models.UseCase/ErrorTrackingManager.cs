using Hohoema.Models.Domain;
using Hohoema.Models.Domain.Helpers;
using Hohoema.Presentation.Services;
using Hohoema.Presentation.Services.Page;
using Hohoema.Presentation.Services.Player;
using Microsoft.AppCenter.Crashes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno.Threading;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace Hohoema.Models.UseCase
{    
    public sealed class ErrorTrackingManager
    {
        private readonly PageManager _pageManager;
        private readonly NiconicoSession _niconicoSession;
        private readonly PrimaryViewPlayerManager _primaryViewPlayerManager;
        private readonly ScondaryViewPlayerManager _scondaryViewPlayerManager;

        public const int MAX_REPORT_COUNT = 10;

        public ErrorTrackingManager(
            PageManager pageManager,
            NiconicoSession niconicoSession,
            PrimaryViewPlayerManager primaryViewPlayerManager,
            ScondaryViewPlayerManager scondaryViewPlayerManager
            )
        {
            _pageManager = pageManager;
            _niconicoSession = niconicoSession;
            _primaryViewPlayerManager = primaryViewPlayerManager;
            _scondaryViewPlayerManager = scondaryViewPlayerManager;
        }

        public Dictionary<string, string> MakeReportParameters()
        {
            var pageName = _pageManager.CurrentPageType.ToString();
            var pageParameter = _pageManager.CurrentPageNavigationParameters is not null ? JsonConvert.SerializeObject(_pageManager.CurrentPageNavigationParameters) : "null";

            return new Dictionary<string, string>
                {
                    { "IsInternetAvailable", InternetConnection.IsInternet().ToString() },
                    { "IsLoggedIn", _niconicoSession.IsLoggedIn.ToString() },
                    { "IsPremiumAccount", _niconicoSession.IsPremiumAccount.ToString() },
                    { "RecentOpenPageName", pageName },
                    { "RecentOpenPageParameters", pageParameter },
                    { "OperatingSystemArchitecture", Microsoft.Toolkit.Uwp.Helpers.SystemInformation.Instance.OperatingSystemArchitecture.ToString() },
                    { "PrimaryWindowPlayerDisplayMode", _primaryViewPlayerManager.DisplayMode.ToString() },
                    { "IsShowSecondaryView", _scondaryViewPlayerManager.IsShowSecondaryView.ToString() },
                };
        }


        public static async Task<ErrorAttachmentLog> CreateScreenshotAttachmentLog(RenderTargetBitmap screenshot)
        {
            using (var memoryStream = new InMemoryRandomAccessStream())
            {
                BitmapEncoder encoder =
                    await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, memoryStream);

                IBuffer pixelBuffer = await screenshot.GetPixelsAsync();
                byte[] pixels = pixelBuffer.ToArray();

                var displayInformation = DisplayInformation.GetForCurrentView();

                encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied,
                    (uint)screenshot.PixelWidth, (uint)screenshot.PixelHeight, displayInformation.RawDpiX, displayInformation.RawDpiY, pixels);
                await encoder.FlushAsync();

                byte[] imageBuffer = new byte[memoryStream.Size];
                await memoryStream.AsStreamForRead().ReadAsync(imageBuffer, 0, (int)memoryStream.Size);

                return ErrorAttachmentLog.AttachmentWithBinary(imageBuffer, "screenshot.png", "image/png");
            }
        }

        public static ErrorAttachmentLog CreateTextAttachmentLog(string text)
        {
            return ErrorAttachmentLog.AttachmentWithText(text, "userInput.txt");
        }

        FastAsyncLock _fastAsyncLock = new FastAsyncLock();
        public void SendReportWithAttatchments(Exception exception, params ErrorAttachmentLog[] logs)
        {
            Crashes.TrackError(exception, MakeReportParameters(), logs);
        }
    }

    public enum ReportSendFailedReason
    {
        FailSendingToAppCenter,
        AlreadyReported,
    }

    public sealed class ReportSendResult
    {
        public static ReportSendResult Failed(ReportSendFailedReason reason) => new ReportSendResult(reason);

        public static ReportSendResult Success(ErrorReport errorReport)
        {
            return new ReportSendResult(errorReport);
        }

        private ReportSendResult(ReportSendFailedReason reason)
        {
            IsSuccess = false;
            FailedReason = reason;
        }

        private ReportSendResult(ErrorReport errorReport)
        {
            IsSuccess = true;
            Report = errorReport;
        }

        public bool IsSuccess { get; }
        public ErrorReport Report { get; }
        public ReportSendFailedReason? FailedReason { get; }
    }
}
