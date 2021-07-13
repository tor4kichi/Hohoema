using Hohoema.Models.Domain.Niconico;
using Hohoema.Models.Helpers;
using Hohoema.Models.UseCase.Niconico.Player;
using Hohoema.Models.UseCase.PageNavigation;
using Microsoft.AppCenter.Crashes;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace Hohoema.Models.UseCase
{
    public static class ErrorTrackingManager
    {
        private static readonly PageManager _pageManager;
        private static readonly NiconicoSession _niconicoSession;
        private static readonly PrimaryViewPlayerManager _primaryViewPlayerManager;
        private static readonly ScondaryViewPlayerManager _scondaryViewPlayerManager;

        public const int MAX_REPORT_COUNT = 10;

        static ErrorTrackingManager()
        {
            _pageManager = App.Current.Container.Resolve<PageManager>();
            _niconicoSession = App.Current.Container.Resolve<NiconicoSession>(); 
            _primaryViewPlayerManager = App.Current.Container.Resolve<PrimaryViewPlayerManager>();
            _scondaryViewPlayerManager = App.Current.Container.Resolve<ScondaryViewPlayerManager>();
        }

        public static Dictionary<string, string> MakeReportParameters()
        {
            var pageName = _pageManager.CurrentPageType.ToString();
            var pageParameter = _pageManager.CurrentPageNavigationParameters is not null ? JsonSerializer.Serialize(_pageManager.CurrentPageNavigationParameters) : "null";

            return new Dictionary<string, string>
            {
                { "IsInternetAvailable", InternetConnection.IsInternet().ToString() },
                { "IsLoggedIn", _niconicoSession.IsLoggedIn.ToString() },
                { "IsPremiumAccount", _niconicoSession.IsPremiumAccount.ToString() },
                { "RecentOpenPageName", pageName },
                { "RecentOpenPageParameters", pageParameter },
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

        public static void TrackUnhandeledError(Windows.ApplicationModel.Core.UnhandledError unhandledError)
        {
            try
            {
                unhandledError.Propagate();
            }
            catch (Exception e)
            {
                TrackError(e);
            }
        }
        public static void TrackError(Exception exception, IDictionary<string, string> parameters = null, params ErrorAttachmentLog[] logs)
        {
            var dict = MakeReportParameters();
            if (parameters != null)
            {
                foreach (var pair in parameters)
                {
                    if (dict.ContainsKey(pair.Key))
                    {
                        dict.Remove(pair.Key);
                    }

                    dict.Add(pair.Key, pair.Value);
                }
            }

            Crashes.TrackError(exception, dict, logs.ToArray());
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
