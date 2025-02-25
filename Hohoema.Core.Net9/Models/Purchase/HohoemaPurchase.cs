#nullable enable
using Hohoema.Helpers;
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Store;
using Windows.Storage;

namespace Hohoema.Models.Purchase;

public class HohoemaPurchase
{
    public const string PurchaseCategory_Cheer = "cheer";
    private static readonly AsyncLock _InitializeLock = new();
    private static bool _Initialized = false;

    static HohoemaPurchase()
    {
        _ = Initialize().ConfigureAwait(false);
    }

    private static async Task Initialize()
    {
        using IDisposable releaser = await _InitializeLock.LockAsync();

        if (_Initialized) { return; }
#if DEBUG
        StorageFile proxyFile = await StorageFile.GetFileFromApplicationUriAsync(
            new Uri("ms-appx:///Assets/StoreTesting.xml")
        );
        await CurrentAppSimulator.ReloadSimulatorAsync(proxyFile);
#endif

        _Initialized = true;
    }

    public static async Task<ListingInformation> GetAvailableCheersAddonsAsync(CancellationToken ct = default)
    {
        using IDisposable releaser = await _InitializeLock.LockAsync(ct);
        ListingInformation listing = await CurrentApp.LoadListingInformationAsync().AsTask(ct);
        return listing;
    }

    public static async Task<PurchaseResults> RequestPurchase(ProductListing addonProduct, CancellationToken ct = default)
    {
        using IDisposable releaser = await _InitializeLock.LockAsync(ct);
        PurchaseResults result = await CurrentApp.RequestProductPurchaseAsync(addonProduct.ProductId).AsTask(ct);
        return result;
    }


    public static bool ProductIsActive(ProductListing product)
    {
        return CurrentApp.LicenseInformation.ProductLicenses[product.ProductId].IsActive;
    }
}
