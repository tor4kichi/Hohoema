using Hohoema.NicoAlert.Helpers;
using Mntone.Nico2.Live;
using NicoPlayerHohoema.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models.Niconico.Live
{
    public class NicoLiveSubscriber
    { 

        HohoemaApp _HohoemaApp;
        AsyncLock _UpdateLock = new AsyncLock();

        public ReadOnlyObservableCollection<Mntone.Nico2.Live.Video.VideoInfo> OnAirStreams { get; }
        public ObservableCollection<Mntone.Nico2.Live.Video.VideoInfo> _OnAirStreams;




        public NicoLiveSubscriber(HohoemaApp hohoemaApp)
        {
            _HohoemaApp = hohoemaApp;

            _OnAirStreams = new ObservableCollection<Mntone.Nico2.Live.Video.VideoInfo>();
            OnAirStreams = new ReadOnlyObservableCollection<Mntone.Nico2.Live.Video.VideoInfo>(_OnAirStreams);

            _HohoemaApp.OnSignin += HohoemaApp_OnSignin;
            _HohoemaApp.OnSignout += _HohoemaApp_OnSignout;

            HohoemaAlertClient.AlertClient.LiveRecieved += AlertClient_LiveRecieved;
        }

        private async void HohoemaApp_OnSignin()
        {
            await UpdateOnAirStreams();
        }

        private void _HohoemaApp_OnSignout()
        {
            _OnAirStreams.Clear();
        }

        private async void AlertClient_LiveRecieved(object sender, Hohoema.NicoAlert.NicoLiveAlertEventArgs e)
        {
            using (var releaser = await _UpdateLock.LockAsync())
            {
                if (_HohoemaApp.FollowManager.IsFollowItem(FollowItemType.Community, e.CommunityId) 
                    || _HohoemaApp.FollowManager.IsFollowItem(FollowItemType.Channel, e.CommunityId)
                    )
                {
                    var liveInfo = await _HohoemaApp.NiconicoContext.Live.GetLiveVideoInfoAsync(e.Id);
                    if (liveInfo.IsOK)
                    {
                        var detail = liveInfo.VideoInfo;
                        _OnAirStreams.Add(liveInfo.VideoInfo);
                    }
                }
            }
        }

        public async Task UpdateOnAirStreams()
        {
            using (var releaser = await _UpdateLock.LockAsync())
            {
                _OnAirStreams.Clear();

                if (_HohoemaApp.IsLoggedIn)
                {
                    var mypageLiveInfo = await _HohoemaApp.NiconicoContext.Live.GetMyPageAsync();

                    foreach (var onAir in mypageLiveInfo.OnAirPrograms)
                    {
                        var liveInfo = await _HohoemaApp.NiconicoContext.Live.GetLiveVideoInfoAsync(onAir.ID);
                        if (liveInfo.IsOK)
                        {
                            var detail = liveInfo.VideoInfo;
                            _OnAirStreams.Add(liveInfo.VideoInfo);
                        }

                        await Task.Delay(100);
                    }
                }

            }
        }
    }
}
