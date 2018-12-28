using Prism.Commands;
using Prism.Mvvm;
using System.Diagnostics;
using System.Windows.Input;
using Microsoft.Practices.Unity;
using NicoPlayerHohoema.Services.Helpers;

namespace NicoPlayerHohoema.Services.Page
{
    public sealed class HohoemaPin : BindableBase
    {
        public HohoemaPageType PageType { get; set; }
        public string Parameter { get; set; }
        public string Label { get; set; }

        private string _OverrideLabel;
        public string OverrideLabel
        {
            get { return _OverrideLabel; }
            set { SetProperty(ref _OverrideLabel, value); }
        }

    }
}
