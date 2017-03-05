using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace NicoPlayerHohoema.Views.Controls
{
    public partial class FloatContentContainer : Control
    {
        public event FloatContentDisplayModeChangedEventHandler DisplayModeChanged;

        private void OnDisplayModeChanged(bool isFillFloatContent, bool isDisplay)
        {
            DisplayModeChanged?.Invoke(this, isFillFloatContent, isDisplay);
        }
    }
}
