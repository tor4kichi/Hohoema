#nullable enable
using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace Hohoema.Views.Converters;

public sealed partial class ItemClickEventArgsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is ItemClickEventArgs args)
        {
            return args.ClickedItem;
        }
        else
        {
            return value;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
