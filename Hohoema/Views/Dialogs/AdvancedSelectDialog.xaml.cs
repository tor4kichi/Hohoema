#nullable enable
using Microsoft.Toolkit.Uwp.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// コンテンツ ダイアログの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace Hohoema.Views.Dialogs;

public sealed partial class AdvancedSelectDialog : ContentDialog
{
    public void SetSourceItems<T>(List<T> source, Func<object, string, bool> filter = null)
    {
        ItemsList.ItemsSource = source;
        _advancedCollectionView.Source = source;
        ItemsList.SelectedItem = source.FirstOrDefault();

        if (filter != null)
        {
            _advancedCollectionView.Filter = (x) => !string.IsNullOrWhiteSpace(FilteringTextBox.Text) ? filter(x, FilteringTextBox.Text) : true;
            FilteringTextBox.Visibility = Visibility.Visible;
        }
        else
        {
            _advancedCollectionView.Filter = null;
            FilteringTextBox.Visibility = Visibility.Collapsed;
        }
    }

    public string ItemDisplayMemberPath
    {
        get { return (string)GetValue(ItemDisplayMemberPathProperty); }
        set { SetValue(ItemDisplayMemberPathProperty, value); }
    }

    // Using a DependencyProperty as the backing store for ItemDisplayMemberPath.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ItemDisplayMemberPathProperty =
        DependencyProperty.Register("ItemDisplayMemberPath", typeof(string), typeof(AdvancedSelectDialog), new PropertyMetadata(default(string)));



    public bool IsMultiSelection
    {
        get { return (bool)GetValue(IsMultiSelectionProperty); }
        set { SetValue(IsMultiSelectionProperty, value); }
    }

    // Using a DependencyProperty as the backing store for IsMultiSelection.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IsMultiSelectionProperty =
        DependencyProperty.Register("IsMultiSelection", typeof(bool), typeof(AdvancedSelectDialog), new PropertyMetadata(true));




    AdvancedCollectionView _advancedCollectionView = new AdvancedCollectionView();



    public AdvancedSelectDialog()
    {
        this.InitializeComponent();
    }

    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
    }

    private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
    }

    public List<object> GetResultItems()
    {
        return ItemsList.SelectedItems.ToList();
    }

    private void FilteringTextBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        ItemsList.SelectedItem = args.ChosenSuggestion;
        ItemsList.ScrollIntoView(args.ChosenSuggestion, ScrollIntoViewAlignment.Leading);
    }

    private void FilteringTextBox_TextChanged_1(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        _advancedCollectionView.RefreshFilter();
    }

    private void ItemsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        IsPrimaryButtonEnabled = ItemsList.SelectedItems.Any();
    }
}
