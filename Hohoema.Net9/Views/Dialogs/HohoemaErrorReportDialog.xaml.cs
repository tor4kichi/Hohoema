#nullable enable
using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

// コンテンツ ダイアログの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace Hohoema.Views.Dialogs;

public sealed partial class HohoemaErrorReportDialog : ContentDialog
{
    private readonly Exception _exception;

    private HohoemaErrorReportDialog(Exception exception, bool sendScreenshot, ImageSource screenshot)
    {
        _exception = exception;
        this.InitializeComponent();

        ExceptionMessageTextBlock.Text = _exception.ToString();
        ExceptionMessageTextBlock.Visibility = Visibility.Visible;
        SendScreenshotToggleButton.IsOn = sendScreenshot;
        ScreenshotImage.Source = screenshot;
    }

    private HohoemaErrorReportDialog(bool sendScreenshot, ImageSource screenshot)
    {
        this.InitializeComponent();
        ExceptionMessageTextBlock.Visibility = Visibility.Collapsed;
        SendScreenshotToggleButton.IsOn = sendScreenshot;
        ScreenshotImage.Source = screenshot;
    }

    public static async Task<HohoemaErrorReportDialogResult> ShowAsync(Exception exception, bool sendScreenshot, ImageSource screenshot)
    {
        var dialog = new HohoemaErrorReportDialog(exception, sendScreenshot, screenshot);
        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            return new HohoemaErrorReportDialogResult
            { 
                IsSendRequested = true,
                Data = new HohoemaErrorReportDialogInputData 
                {
                    InputText = dialog.UserInputTextBox.Text,
                    UseScreenshot = dialog.SendScreenshotToggleButton.IsOn
                }
            };
        }
        else
        {
            return new HohoemaErrorReportDialogResult { IsSendRequested = false };
        }
    }

    public static async Task<HohoemaErrorReportDialogResult> ShowAsyncForLiteIssue(bool sendScreenshot, ImageSource screenshot)
    {
        var dialog = new HohoemaErrorReportDialog(sendScreenshot, screenshot);
        
        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            return new HohoemaErrorReportDialogResult
            {
                IsSendRequested = true,
                Data = new HohoemaErrorReportDialogInputData
                {
                    InputText = dialog.UserInputTextBox.Text,
                    UseScreenshot = dialog.SendScreenshotToggleButton.IsOn
                }
            };
        }
        else
        {
            return new HohoemaErrorReportDialogResult { IsSendRequested = false };
        }
    }
}

public struct HohoemaErrorReportDialogResult
{
    public bool IsSendRequested { get; set; }

    public HohoemaErrorReportDialogInputData Data { get; set; }
}

public sealed class HohoemaErrorReportDialogInputData
{
    public string InputText { get; set; }

    public bool UseScreenshot { get; set; }        
}
