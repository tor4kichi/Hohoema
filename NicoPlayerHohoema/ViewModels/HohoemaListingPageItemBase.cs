using NicoPlayerHohoema.Interfaces;
using NicoPlayerHohoema.Models;
using Prism.Commands;
using Prism.Mvvm;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI;

namespace NicoPlayerHohoema.ViewModels
{
    public class Selectable : BindableBase
    {
        private bool _IsSelected;
        public bool IsSelected
        {
            get { return _IsSelected; }
            set { SetProperty(ref _IsSelected, value); }
        }
    }


	public abstract class HohoemaListingPageItemBase : Selectable, Interfaces.IHohoemaListItem, IDisposable
	{
        public void Dispose()
        {
            CancelDefrredUpdate();

            OnDispose();
        }

        protected virtual void OnDispose() { }

        #region IHohoemaListItem 

        private string _Label;
        public string Label
        {
            get { return _Label; }
            set
            {
                if (SetProperty(ref _Label, value))
                {
                    RaisePropertyChanged(nameof(HasTitle));
                }
            }
        }
        public bool HasTitle => !string.IsNullOrWhiteSpace(Label);

        private string _Description;
        public string Description
        {
            get { return _Description; }
            set
            {
                if (SetProperty(ref _Description, value))
                {
                    RaisePropertyChanged(nameof(HasDescription));
                }
            }
        }
        public bool HasDescription => !string.IsNullOrWhiteSpace(Description);

        private string _OptionText;
        public string OptionText
        {
            get { return _OptionText; }
            set
            {
                if (SetProperty(ref _OptionText, value))
                {
                    RaisePropertyChanged(nameof(HasOptionText));
                }
            }
        }
        public bool HasOptionText => !string.IsNullOrWhiteSpace(OptionText);
        
        public string FirstImageUrl => ImageUrls?.FirstOrDefault();
        public bool HasImageUrl => !string.IsNullOrWhiteSpace(ImageUrls?.FirstOrDefault());
        public bool IsMultipulImages => ImageUrls?.Count > 0;

        private string _ImageCaption;
        public string ImageCaption
        {
            get { return _ImageCaption; }
            set
            {
                if (SetProperty(ref _ImageCaption, value))
                {
                    RaisePropertyChanged(nameof(HasImageCaption));
                }
            }
        }
        public bool HasImageCaption => !string.IsNullOrWhiteSpace(ImageCaption);


        private Color _ThemeColor = Colors.Transparent;
        public Color ThemeColor
        {
            get { return _ThemeColor; }
            set
            {
                SetProperty(ref _ThemeColor, value);
            }
        }


        private bool _IsVisible = true;
        public bool IsVisible
        {
            get { return _IsVisible; }
            set
            {
                SetProperty(ref _IsVisible, value);
            }
        }


        private string _InvisibleDescription;
        public string InvisibleDescription
        {
            get { return _InvisibleDescription; }
            set
            {
                SetProperty(ref _InvisibleDescription, value);
            }
        }


        private string _PrimaryActionTitle;
        public string PrimaryActionTitle
        {
            get { return _PrimaryActionTitle; }
            set
            {
                SetProperty(ref _PrimaryActionTitle, value);
            }
        }

        public List<ActionSet> SecondaryActions { get; private set; } = new List<ActionSet>();

        #endregion


        private ObservableCollection<string> ImageUrlsSource { get; set; } = new ObservableCollection<string>();
        public ReadOnlyObservableCollection<string> ImageUrls { get; private set; }

        protected void AddImageUrl(string url)
        {
            ImageUrlsSource.Add(url);
            RaisePropertyChanged(nameof(FirstImageUrl));
            RaisePropertyChanged(nameof(HasImageUrl));
            RaisePropertyChanged(nameof(IsMultipulImages));
        }

        public HohoemaListingPageItemBase()
        {
            ImageUrls = new ReadOnlyObservableCollection<string>(ImageUrlsSource);

        }

        public Task DeferredUpdate()
        {
            return OnDeferredUpdate();
        }

        protected virtual Task OnDeferredUpdate() { return Task.CompletedTask; }


        public void CancelDefrredUpdate()
        {
            OnCancelDeferrdUpdate();
        }

        protected virtual void OnCancelDeferrdUpdate()
        {

        }
    }
}
