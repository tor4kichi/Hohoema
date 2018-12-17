using Hohoema.NicoAlert.Helpers;
using Mntone.Nico2.Live;
using NicoPlayerHohoema.Interfaces;
using NicoPlayerHohoema.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Services
{
    public class NicoLiveSubscriber
    { 

        AsyncLock _UpdateLock = new AsyncLock();

        public ReadOnlyObservableCollection<Mntone.Nico2.Live.Video.VideoInfo> OnAirStreams { get; }
        public ObservableCollection<Mntone.Nico2.Live.Video.VideoInfo> _OnAirStreams;

        public ReadOnlyObservableCollection<Mntone.Nico2.Live.Video.VideoInfo> ReservedStreams { get; }
        public ObservableCollection<Mntone.Nico2.Live.Video.VideoInfo> _ReservedStreams;

        public NiconicoSession NiconicoSession { get; }
        public FollowManager FollowManager { get; }
        public HohoemaAlertClient HohoemaAlertClient { get; }

        public NicoLiveSubscriber(
            NiconicoSession niconicoSession, 
            FollowManager followManager,
            HohoemaAlertClient hohoemaAlertClient
            )
        {
            NiconicoSession = niconicoSession;
            FollowManager = followManager;
            HohoemaAlertClient = hohoemaAlertClient;
            _OnAirStreams = new ObservableCollection<Mntone.Nico2.Live.Video.VideoInfo>();
            OnAirStreams = new ReadOnlyObservableCollection<Mntone.Nico2.Live.Video.VideoInfo>(_OnAirStreams);

            _ReservedStreams = new ObservableCollection<Mntone.Nico2.Live.Video.VideoInfo>();
            ReservedStreams = new ReadOnlyObservableCollection<Mntone.Nico2.Live.Video.VideoInfo>(_ReservedStreams);

            NiconicoSession.LogIn += (sender, e) => 
            {
                _ = UpdateOnAirStreams();
            };
            NiconicoSession.LogOut += (sender, e) => 
            {
                _OnAirStreams.Clear();
                _ReservedStreams.Clear();
            };

            HohoemaAlertClient.AlertClient.LiveRecieved += AlertClient_LiveRecieved;
        }

        private async void AlertClient_LiveRecieved(object sender, Hohoema.NicoAlert.NicoLiveAlertEventArgs e)
        {
            using (var releaser = await _UpdateLock.LockAsync())
            {
                if (FollowManager.IsFollowItem(FollowItemType.Community, e.CommunityId) 
                    || FollowManager.IsFollowItem(FollowItemType.Channel, e.CommunityId)
                    )
                {
                    var liveInfo = await NiconicoSession.Context.Live.GetLiveVideoInfoAsync(e.Id);
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

                if (NiconicoSession.IsLoggedIn)
                {
                    var mypageLiveInfo = await NiconicoSession.Context.Live.GetMyPageAsync();

                    foreach (var onAir in mypageLiveInfo.OnAirPrograms)
                    {
                        var liveInfo = await NiconicoSession.Context.Live.GetLiveVideoInfoAsync(onAir.ID);
                        if (liveInfo.IsOK)
                        {
                            var detail = liveInfo.VideoInfo;
                            _OnAirStreams.Add(liveInfo.VideoInfo);
                        }

                        await Task.Delay(100);
                    }

                    foreach (var reserved in mypageLiveInfo.ReservedPrograms)
                    {
                        var liveInfo = await NiconicoSession.Context.Live.GetLiveVideoInfoAsync(reserved.ID);
                        if (liveInfo.IsOK)
                        {
                            var detail = liveInfo.VideoInfo;
                            _ReservedStreams.Add(liveInfo.VideoInfo);
                        }

                        await Task.Delay(100);
                    }
                }

            }
        }
    }
}
