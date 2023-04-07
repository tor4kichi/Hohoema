using Hohoema.Models.Application;
using Hohoema.Models.PageNavigation;
using Hohoema.Models.Pins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.UseCase.Migration
{
    public class SearchPageQueryMigrate_0_26_0 : IMigrateSync
    {
        private readonly AppFlagsRepository _appFlagsRepository;
        private readonly PinSettings _pinSettings;

        public SearchPageQueryMigrate_0_26_0(
            AppFlagsRepository appFlagsRepository,
            PinSettings pinSettings
            )
        {
            _appFlagsRepository = appFlagsRepository;
            _pinSettings = pinSettings;
        }

        public void Migrate()
        {
            if (_appFlagsRepository.IsSearchQueryInPinsMigration_V_0_26_0) { return; }

            var pins = _pinSettings.ReadAllItems();
            foreach (var pin in pins)
            {
#pragma warning disable CS0612 // 型またはメンバーが旧型式です
                if (pin.PageType == HohoemaPageType.SearchResultKeyword)
                {
                    pin.PageType = HohoemaPageType.Search;
                    pin.Parameter = pin.Parameter + $"&service={SearchTarget.Keyword}";
                    _pinSettings.UpdateItem(pin);
                }
                else if (pin.PageType == HohoemaPageType.SearchResultTag)
                {
                    pin.PageType = HohoemaPageType.Search;
                    pin.Parameter = pin.Parameter + $"&service={SearchTarget.Tag}";
                    _pinSettings.UpdateItem(pin);
                }
                else if (pin.PageType == HohoemaPageType.SearchResultLive)
                {
                    pin.PageType = HohoemaPageType.Search;
                    pin.Parameter = pin.Parameter + $"&service={SearchTarget.Niconama}";
                    _pinSettings.UpdateItem(pin);
                }
#pragma warning restore CS0612 // 型またはメンバーが旧型式です
            }

            _appFlagsRepository.IsSearchQueryInPinsMigration_V_0_26_0 = true;
        }
    }
}
