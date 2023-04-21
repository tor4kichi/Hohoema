#nullable enable
using Hohoema.Contracts.Subscriptions;
using NiconicoToolkit.NicoRepo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Hohoema.Views.Hohoema.Subscription;

public sealed partial class EditSubscriptionGroupDialog : ContentDialog
{
    public EditSubscriptionGroupDialog()
    {
        this.InitializeComponent();
    }

    public new IAsyncOperation<ContentDialogResult> ShowAsync() 
    {
        throw new InvalidOperationException();
    }

    public new IAsyncOperation<ContentDialogResult> ShowAsync(ContentDialogPlacement contentDialogPlacement)
    {
        throw new InvalidOperationException();
    }

    public async Task<SubscriptionGroupCreateResult> ShowAsync(
        string title,
        bool isAutoUpdateDefault,
        bool isAddToQueueeDefault,
        bool isToastNotificationDefault,
        bool isShowMenuItemDefault,
        Func<string, bool> titleValidater
        )
    {
        TextBox_SubscGroupTitle.Text = title;
        CheckBox_AutoUpdate.IsChecked = isAutoUpdateDefault;
        CheckBox_AddToQueue.IsChecked = isAddToQueueeDefault;
        CheckBox_ToastNotification.IsChecked = isToastNotificationDefault;
        CheckBox_ShowMenuItem.IsChecked = isShowMenuItemDefault;
        IsPrimaryButtonEnabled = titleValidater(title);

        void TextBox_SubscGroupTitle_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.IsPrimaryButtonEnabled = titleValidater((sender as TextBox)!.Text);
        }

        TextBox_SubscGroupTitle.TextChanged += TextBox_SubscGroupTitle_TextChanged;
        
        try
        {
            if (await base.ShowAsync() == ContentDialogResult.Primary)
            {
                return new SubscriptionGroupCreateResult
                {
                    IsSuccess = true,
                    Title = TextBox_SubscGroupTitle.Text,
                    IsAutoUpdate = CheckBox_AutoUpdate.IsChecked is true,
                    IsAddToQueue = CheckBox_AddToQueue.IsChecked is true,
                    IsToastNotification = CheckBox_ToastNotification.IsChecked is true,
                    IsShowMenuItem = CheckBox_ShowMenuItem.IsChecked is true,
                };
            }
            else
            {
                return new SubscriptionGroupCreateResult { IsSuccess = false };
            }
        }
        finally
        {
            TextBox_SubscGroupTitle.TextChanged -= TextBox_SubscGroupTitle_TextChanged;
        }
    }
}
