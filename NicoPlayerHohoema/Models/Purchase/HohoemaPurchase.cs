using NicoPlayerHohoema.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Store;
using Windows.Storage;

namespace NicoPlayerHohoema.Models.Purchase
{
    public class HohoemaPurchase
    {
        public const string PurchaseCategory_Cheer = "cheer";

        static AsyncLock _InitializeLock = new AsyncLock();
        static bool _Initialized = false;

        static HohoemaPurchase()
        {
            Initialize().ConfigureAwait(false);
        }

        private static async Task Initialize()
        {
            using (var releaser = await _InitializeLock.LockAsync())
            {
#if DEBUG
                var proxyFile = await StorageFile.GetFileFromApplicationUriAsync(
                    new Uri("ms-appx:///Assets/StoreTesting.xml")
                );
                await CurrentAppSimulator.ReloadSimulatorAsync(proxyFile);
#endif

                _Initialized = true;
            }
        }

        public static async Task<ListingInformation> GetAvailableCheersAddOn()
        {
            using (var releaser = await _InitializeLock.LockAsync())
            {
                var listing = await CurrentApp.LoadListingInformationAsync();
                return listing;
            }
        }

        public static async Task<PurchaseResults> RequestPurchase(ProductListing addonProduct)
        {
            using (var releaser = await _InitializeLock.LockAsync())
            {
                var result = await CurrentApp.RequestProductPurchaseAsync(addonProduct.ProductId);
                return result;
            }
        }


        public static bool ProductIsActive(ProductListing product)
        {
            return CurrentApp.LicenseInformation.ProductLicenses[product.ProductId].IsActive;
        }
    }
}
