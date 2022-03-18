
using Hohoema.Models.Domain;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI;

namespace Hohoema.Presentation.ViewModels
{
    public class Selectable : ObservableObject
    {
        private bool _IsSelected;
        public bool IsSelected
        {
            get { return _IsSelected; }
            set { SetProperty(ref _IsSelected, value); }
        }
    }


	public abstract class HohoemaListingPageItemBase : Selectable, IHohoemaListItem, IDisposable
	{
        protected bool IsDisposed { get; private set; } = false;
        public void Dispose()
        {
            IsDisposed = true;
            
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
                    OnPropertyChanged(nameof(HasTitle));
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
                    OnPropertyChanged(nameof(HasDescription));
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
                    OnPropertyChanged(nameof(HasOptionText));
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
                    OnPropertyChanged(nameof(HasImageCaption));
                }
            }
        }
        public bool HasImageCaption => !string.IsNullOrWhiteSpace(ImageCaption);

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
            OnPropertyChanged(nameof(FirstImageUrl));
            OnPropertyChanged(nameof(HasImageUrl));
            OnPropertyChanged(nameof(IsMultipulImages));
        }

        public HohoemaListingPageItemBase()
        {
            ImageUrls = new ReadOnlyObservableCollection<string>(ImageUrlsSource);

        }
    }
}
