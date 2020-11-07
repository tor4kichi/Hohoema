using Hohoema.Models.Domain.Application;
using Hohoema.Models.Domain.PageNavigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.UseCase.Migration
{
    public class SearchPageQueryMigrate_0_26_0 : IMigrate
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
                if (pin.PageType == Domain.PageNavigation.HohoemaPageType.SearchResultKeyword)
                {
                    pin.PageType = Domain.PageNavigation.HohoemaPageType.Search;
                    pin.Parameter = pin.Parameter + $"&service={SearchTarget.Keyword}";
                    _pinSettings.UpdateItem(pin);
                }
                else if (pin.PageType == Domain.PageNavigation.HohoemaPageType.SearchResultTag)
                {
                    pin.PageType = Domain.PageNavigation.HohoemaPageType.Search;
                    pin.Parameter = pin.Parameter + $"&service={SearchTarget.Tag}";
                    _pinSettings.UpdateItem(pin);
                }
                else if (pin.PageType == Domain.PageNavigation.HohoemaPageType.SearchResultMylist)
                {
                    pin.PageType = Domain.PageNavigation.HohoemaPageType.Search;
                    pin.Parameter = pin.Parameter + $"&service={SearchTarget.Mylist}";
                    _pinSettings.UpdateItem(pin);
                }
                else if (pin.PageType == Domain.PageNavigation.HohoemaPageType.SearchResultLive)
                {
                    pin.PageType = Domain.PageNavigation.HohoemaPageType.Search;
                    pin.Parameter = pin.Parameter + $"&service={SearchTarget.Niconama}";
                    _pinSettings.UpdateItem(pin);
                }
                else if (pin.PageType == Domain.PageNavigation.HohoemaPageType.SearchResultCommunity)
                {
                    pin.PageType = Domain.PageNavigation.HohoemaPageType.Search;
                    pin.Parameter = pin.Parameter + $"&service={SearchTarget.Community}";
                    _pinSettings.UpdateItem(pin);
                }
#pragma warning restore CS0612 // 型またはメンバーが旧型式です
            }

            _appFlagsRepository.IsSearchQueryInPinsMigration_V_0_26_0 = true;
        }
    }
}
