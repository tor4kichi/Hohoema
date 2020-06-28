﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace Hohoema.Views.Converters
{
    public sealed class SwipeSeekValueToTimeSpan : IValueConverter
    {
        const double SeekScale = 1; 

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is double)
            {
                var d = (double)value;
                return TimeSpan.FromSeconds(d * SeekScale);
            }

            return TimeSpan.Zero;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
