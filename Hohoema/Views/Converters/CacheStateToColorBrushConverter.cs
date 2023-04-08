using Hohoema.Models.Player.Video.Cache;
using System;
using Windows.UI;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace Hohoema.Views.Converters;

public class CacheStateToColorBrushConverter : IValueConverter
{
    
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is NicoVideoCacheState)
        {
            var state = (NicoVideoCacheState)value;
            switch (state)
            {
                case NicoVideoCacheState.NotCacheRequested:
                    return new SolidColorBrush(Colors.Transparent);
                case NicoVideoCacheState.Pending:
                    return new SolidColorBrush(Colors.Gray);
                case NicoVideoCacheState.Downloading:
                    return new SolidColorBrush(Colors.Blue);
                case NicoVideoCacheState.Cached:
                    return new SolidColorBrush(Colors.Green);
                default:
                    break;
            }

        }

        return new SolidColorBrush(Colors.Transparent);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is SolidColorBrush)
        {
            var color = (value as SolidColorBrush).Color;
            // TODO: 

        }

        return NicoVideoCacheState.NotCacheRequested;
    }
}
