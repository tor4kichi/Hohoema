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
	public abstract class HohoemaListingPageItemBase : BindableBase, Models.IHohoemaListItem, IDisposable
	{
        IDisposable _Disposer;
        public void Dispose()
        {
            OnDispose();

            _Disposer?.Dispose();
            _Disposer = null;
        }

        protected virtual void OnDispose() { }

        #region IHohoemaListItem 

        private string _Title;
        public string Title
        {
            get { return _Title; }
            set
            {
                if (SetProperty(ref _Title, value))
                {
                    OnPropertyChanged(nameof(HasTitle));
                }
            }
        }
        public bool HasTitle => !string.IsNullOrWhiteSpace(Title);

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

        private string _PrimaryActionTitle;
        public string PrimaryActionTitle
        {
            get { return _PrimaryActionTitle; }
            set
            {
                SetProperty(ref _PrimaryActionTitle, value);
            }
        }

        abstract public ICommand PrimaryCommand { get; }


        public List<ActionSet> SecondaryActions { get; private set; } = new List<ActionSet>();

        #endregion


        protected ObservableCollection<string> ImageUrlsSource { get; private set; } = new ObservableCollection<string>();
        public ReadOnlyObservableCollection<string> ImageUrls { get; private set; }

        public HohoemaListingPageItemBase()
        {
            ImageUrls = new ReadOnlyObservableCollection<string>(ImageUrlsSource);

            _Disposer = ImageUrls.CollectionChangedAsObservable()
                .Subscribe(_ =>
                {
                    OnPropertyChanged(nameof(FirstImageUrl));
                    OnPropertyChanged(nameof(HasImageUrl));
                    OnPropertyChanged(nameof(IsMultipulImages));
                });
        }
    }
}
