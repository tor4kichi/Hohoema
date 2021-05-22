using I18NPortable;
using NiconicoToolkit.Live;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hohoema.Models.UseCase.PageNavigation;
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Models.Domain.Niconico.Live;
using Hohoema.Models.Domain.Niconico;

namespace Hohoema.Presentation.ViewModels.PrimaryWindowCoreLayout
{
    public class LiveMenuSubPageContent : MenuItemBase
    {

        public LiveMenuSubPageContent(
            NiconicoSession niconicoSession,
            PageManager pageManager
            )
        {
            NiconicoSession = niconicoSession;
            PageManager = pageManager;
            MenuItems = new ObservableCollection<HohoemaListingPageItemBase>();

            NiconicoSession.LogIn += (_, __) => ResetItems();
            NiconicoSession.LogOut += (_, __) => ResetItems();

            ResetItems();
        }

        private async void ResetItems()
        {
            using (await NiconicoSession.SigninLock.LockAsync())
            {
                MenuItems.Clear();

                if (NiconicoSession.IsLoggedIn)
                {
                    MenuItems.Add(new MenuItemViewModel(HohoemaPageType.Timeshift.Translate(), HohoemaPageType.Timeshift));
                    MenuItems.Add(new MenuItemViewModel(HohoemaPageType.NicoRepo.Translate(), HohoemaPageType.NicoRepo));
                    MenuItems.Add(new MenuItemViewModel(HohoemaPageType.FollowManage.Translate(), HohoemaPageType.FollowManage));
                }

                RaisePropertyChanged(nameof(MenuItems));
            }
        }


        public NiconicoSession NiconicoSession { get; }
        public PageManager PageManager { get; }

        public ObservableCollection<HohoemaListingPageItemBase> MenuItems { get; private set; }

    }

    public class OnAirStream : ILiveContent
    {
        public string BroadcasterId { get; internal set; }
        public string Id { get; internal set; }
        public string Label { get; internal set; }

        public string CommunityName { get; internal set; }
        public string Thumbnail { get; internal set; }

        public DateTimeOffset StartAt { get; internal set; }

        public string ProviderId => BroadcasterId;

        public string ProviderName => CommunityName;

        public ProviderType ProviderType => ProviderType.Community; // TODO
    }
}
