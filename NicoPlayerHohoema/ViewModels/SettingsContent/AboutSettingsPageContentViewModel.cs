using NicoPlayerHohoema.Models;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Store;
using Windows.Storage;
using Windows.UI.Xaml;

namespace NicoPlayerHohoema.ViewModels
{
    public class AboutSettingsPageContentViewModel : SettingsPageContentViewModel
    {
        public string VersionText { get; private set; }

        public List<LisenceItemViewModel> LisenceItems { get; private set; }

        public List<ProductViewModel> PurchaseItems { get; private set; }

        public static List<Version> UpdateNoticeList { get; } = new List<Version>()
        {
            new Version(0, 10),
            new Version(0, 9),
        };

        public AboutSettingsPageContentViewModel()
            : base("このアプリについて", HohoemaSettingsKind.About)
        {
            var version = Windows.ApplicationModel.Package.Current.Id.Version;
            VersionText = $"{version.Major}.{version.Minor}.{version.Build}";

            
            var dispatcher = Window.Current.CoreWindow.Dispatcher;
            LisenceSummary.Load()
                .ContinueWith(async prevResult =>
                {
                    await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        var lisenceSummary = prevResult.Result;

                        LisenceItems = lisenceSummary.Items
                            .OrderBy(x => x.Name)
                            .Select(x => new LisenceItemViewModel(x))
                            .ToList();
                        RaisePropertyChanged(nameof(LisenceItems));
                    });
                });
        }

        protected override async void OnEnter(ICollection<IDisposable> focusingDispsable)
        {
            try
            {
                var listing = await Models.Purchase.HohoemaPurchase.GetAvailableCheersAddOn();
                PurchaseItems = listing.ProductListings.Select(x => new ProductViewModel(x.Value)).ToList();
                RaisePropertyChanged(nameof(PurchaseItems));
            }
            catch { }

            base.OnEnter(focusingDispsable);
        }

        private DelegateCommand<Version> _ShowUpdateNoticeCommand;
        public DelegateCommand<Version> ShowUpdateNoticeCommand
        {
            get
            {
                return _ShowUpdateNoticeCommand
                    ?? (_ShowUpdateNoticeCommand = new DelegateCommand<Version>(async (version) =>
                    {
                        var allVersions = await Models.AppUpdateNotice.GetUpdateNoticeAvairableVersionsAsync();
                        var versions = allVersions.Where(x => x.Major == version.Major && x.Minor == version.Minor).ToList();
                        var text = await Models.AppUpdateNotice.GetUpdateNotices(versions);
                        var dialog = new Views.Service.MarkdownTextDialog();
                        dialog.Title = $"v{version.Major}.{version.Minor} 更新情報 一覧";
                        dialog.Text = text;
                        dialog.PrimaryButtonText = "OK";
                        await dialog.ShowAsync();
                    }));
            }
        }

        private DelegateCommand<ProductViewModel> _ShowCheerPurchaseCommand;
        public DelegateCommand<ProductViewModel> ShowCheerPurchaseCommand
        {
            get
            {
                return _ShowCheerPurchaseCommand
                    ?? (_ShowCheerPurchaseCommand = new DelegateCommand<ProductViewModel>(async (product) => 
                    {
                        var result = await Models.Purchase.HohoemaPurchase.RequestPurchase(product.ProductListing);

                        product.Update();

                        Debug.WriteLine(result.ToString());
                    }));
            }
        }   
    }

    public class ProductViewModel : BindableBase
    {
        private bool _IsActive;
        public bool IsActive
        {
            get { return _IsActive; }
            set { SetProperty(ref _IsActive, value); }
        }

        public ProductListing ProductListing { get; set; }
        public ProductViewModel(ProductListing product)
        {
            ProductListing = product;

            Update();
        }

        internal void Update()
        {
            IsActive = Models.Purchase.HohoemaPurchase.ProductIsActive(ProductListing);
        }
    }



    public class LisenceItemViewModel
    {
        public LisenceItemViewModel(LisenceItem item)
        {
            Name = item.Name;
            Site = item.Site;
            Authors = item.Authors.ToList();
            LisenceType = LisenceTypeToText(item.LisenceType.Value);
            LisencePageUrl = item.LisencePageUrl;
        }

        public string Name { get; private set; }
        public Uri Site { get; private set; }
        public List<string> Authors { get; private set; }
        public string LisenceType { get; private set; }
        public Uri LisencePageUrl { get; private set; }

        string _LisenceText;
        public string LisenceText
        {
            get
            {
                return _LisenceText
                    ?? (_LisenceText = LoadLisenceText());
            }
        }

        string LoadLisenceText()
        {
            string path = Windows.ApplicationModel.Package.Current.InstalledLocation.Path + "\\Assets\\LibLisencies\\" + Name + ".txt";

            try
            {
                var file = StorageFile.GetFileFromPathAsync(path).AsTask();

                file.Wait(3000);

                var task = FileIO.ReadTextAsync(file.Result).AsTask();
                task.Wait(1000);
                return task.Result;
            }
            catch
            {
                return "";
            }
        }


        private string LisenceTypeToText(LisenceType type)
        {
            switch (type)
            {
                case Models.LisenceType.MIT:
                    return "MIT";
                case Models.LisenceType.MS_PL:
                    return "Microsoft Public Lisence";
                case Models.LisenceType.Apache_v2:
                    return "Apache Lisence version 2.0";
                case Models.LisenceType.GPL_v3:
                    return "GNU General Public License Version 3";
                case Models.LisenceType.Simplified_BSD:
                    return "二条項BSDライセンス";
                case Models.LisenceType.CC_BY_40:
                    return "クリエイティブ・コモンズ 表示 4.0 国際";
                case Models.LisenceType.SIL_OFL_v1_1:
                    return "SIL OPEN FONT LICENSE Version 1.1";
                default:
                    throw new NotSupportedException(type.ToString());
            }
        }

    }
}
