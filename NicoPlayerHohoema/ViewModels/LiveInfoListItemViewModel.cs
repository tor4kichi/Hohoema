using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mntone.Nico2.Live;
using Mntone.Nico2.Live.Reservation;
using Mntone.Nico2.Searches.Live;
using NicoPlayerHohoema.Database;
using NicoPlayerHohoema.Interfaces;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Services;

namespace NicoPlayerHohoema.ViewModels
{
    public class LiveInfoListItemViewModel : HohoemaListingPageItemBase, Interfaces.ILiveContent, Views.Extensions.ListViewBase.IDeferInitialize
    {
        public LiveInfoListItemViewModel(
            Services.HohoemaPlaylist hohoemaPlaylist,
            PageManager pageManager,
            ExternalAccessService externalAccessService
            )         
        {
            HohoemaPlaylist = hohoemaPlaylist;
            PageManager = pageManager;
            ExternalAccessService = externalAccessService;
        }

        public Services.HohoemaPlaylist HohoemaPlaylist { get; }
        public PageManager PageManager { get; }
        public ExternalAccessService ExternalAccessService { get; }
        public Mntone.Nico2.Live.ReservationsInDetail.Program Reservation { get; private set; }


        public string LiveId { get; private set; }

		public string CommunityName { get; private set; }
		public string CommunityThumbnail { get; private set; }
		public string CommunityGlobalId { get; private set; }
		public Mntone.Nico2.Live.CommunityType CommunityType { get; private set; }

		public string LiveTitle { get; private set; }
		public int ViewCounter { get; private set; }
		public int CommentCount { get; private set; }
		public DateTimeOffset OpenTime { get; private set; }
		public DateTimeOffset StartTime { get; private set; }
		public bool HasEndTime { get; private set; }
		public DateTimeOffset EndTime { get; private set; }
		public string DurationText { get; private set; }
		public bool IsTimeshiftEnabled { get; private set; }
		public bool IsCommunityMemberOnly { get; private set; }

        public bool IsXbox => Services.Helpers.DeviceTypeHelper.IsXbox;



        public bool NowLive => Elements.Any(x => x == LiveContentElement.Status_Open || x == LiveContentElement.Status_Start);

        public bool IsReserved => Elements.Any(x => x == LiveContentElement.Timeshift_Preserved || x == LiveContentElement.Timeshift_Watch);
        public bool IsTimedOut => Elements.Any(x => x == LiveContentElement.Timeshift_OutDated);

        public List<LiveContentElement> Elements { get; } = new List<LiveContentElement>();
        public DateTimeOffset ExpiredAt { get; internal set; }
        public Mntone.Nico2.Live.ReservationsInDetail.ReservationStatus? ReservationStatus { get; internal set; }




        string ILiveContent.ProviderId => CommunityGlobalId;

        string ILiveContent.ProviderName => CommunityName;

        CommunityType ILiveContent.ProviderType => CommunityType;

        string INiconicoObject.Id => LiveId;

        string INiconicoObject.Label => LiveTitle;




        bool Views.Extensions.ListViewBase.IDeferInitialize.IsInitialized { get; set; }
        
        Task Views.Extensions.ListViewBase.IDeferInitialize.DeferInitializeAsync()
        {
            ResetElements();
            return Task.CompletedTask;
        }



        public void SetReservation(Mntone.Nico2.Live.ReservationsInDetail.Program reservationInfo)
        {
            Reservation = reservationInfo;
            ReservationStatus = NowLive ? null : reservationInfo?.GetReservationStatus();
        }

        public void Setup(Mntone.Nico2.Live.Recommend.LiveRecommendData liveVideoInfo)
        {
            LiveId = "lv" + liveVideoInfo.ProgramId;

            CommunityThumbnail = liveVideoInfo.ThumbnailUrl;

            CommunityGlobalId = liveVideoInfo.DefaultCommunity;
            CommunityType = liveVideoInfo.ProviderType;

            LiveTitle = liveVideoInfo.Title;
            OpenTime = liveVideoInfo.OpenTime;
            StartTime = liveVideoInfo.StartTime;

            //IsTimeshiftEnabled = liveVideoInfo.Video.TimeshiftEnabled;
            //IsCommunityMemberOnly = liveVideoInfo.Video.CommunityOnly;

            AddImageUrl(CommunityThumbnail);

            //Description = $"来場者:{ViewCounter} コメ:{CommentCount}";

            switch (liveVideoInfo.CurrentStatus)
            {
                case Mntone.Nico2.Live.StatusType.Invalid:
                    break;
                case Mntone.Nico2.Live.StatusType.OnAir:
                    DurationText = $"{StartTime - DateTimeOffset.Now} 経過";
                    break;
                case Mntone.Nico2.Live.StatusType.ComingSoon:
                    DurationText = $"開始予定: {StartTime.LocalDateTime.ToString("g")}";
                    break;
                case Mntone.Nico2.Live.StatusType.Closed:
                    DurationText = $"放送終了";
                    break;
                default:
                    break;
            }

            OptionText = DurationText;

            var endTime = liveVideoInfo.CurrentStatus == StatusType.Closed ? DateTimeOffset.Now + TimeSpan.FromMinutes(60) : DateTime.MaxValue;
        }


        public void Setup(VideoInfo liveVideoInfo)
        {
            LiveId = liveVideoInfo.Video.Id;
            CommunityName = liveVideoInfo.Community?.Name;
            if (liveVideoInfo.Community?.Thumbnail != null)
            {
                CommunityThumbnail = liveVideoInfo.Community?.Thumbnail;
            }
            else
            {
                CommunityThumbnail = liveVideoInfo.Video.ThumbnailUrl;
            }
            CommunityGlobalId = liveVideoInfo.Community?.GlobalId;
            CommunityType = liveVideoInfo.Video.ProviderType;

            LiveTitle = liveVideoInfo.Video.Title;
            ViewCounter = int.Parse(liveVideoInfo.Video.ViewCounter);
            CommentCount = int.Parse(liveVideoInfo.Video.CommentCount);
            OpenTime = new DateTimeOffset(liveVideoInfo.Video.OpenTime, TimeSpan.FromHours(9));
            StartTime = new DateTimeOffset(liveVideoInfo.Video.StartTime, TimeSpan.FromHours(9));
            EndTime = new DateTimeOffset(liveVideoInfo.Video.EndTime, TimeSpan.FromHours(9));
            IsTimeshiftEnabled = liveVideoInfo.Video.TimeshiftEnabled;
            IsCommunityMemberOnly = liveVideoInfo.Video.CommunityOnly;

            Label = liveVideoInfo.Video.Title;
            AddImageUrl(CommunityThumbnail);

            Description = $"来場者:{ViewCounter} コメ:{CommentCount}";

            if (StartTime > DateTimeOffset.Now)
            {
                // 予約
                DurationText = $" 開始予定: {StartTime.LocalDateTime.ToString("g")}";
            }
            else if (EndTime > DateTimeOffset.Now)
            {
                var duration = DateTimeOffset.Now - StartTime;
                // 放送中
                if (duration.Hours > 0)
                {
                    DurationText = $"{duration.Hours}時間 {duration.Minutes}分 経過";
                }
                else
                {
                    DurationText = $"{duration.Minutes}分 経過";
                }
            }
            else
            {
                var duration = EndTime - StartTime;
                // 終了
                if (duration.Hours > 0)
                {
                    DurationText = $"{EndTime.ToString("g")} 終了（{duration.Hours}時間 {duration.Minutes}分）";
                }
                else
                {
                    DurationText = $"{EndTime.ToString("g")} 終了（{duration.Minutes}分）";
                }
            }

            OptionText = DurationText;
        }

        public void Setup(NicoLive liveData)
        {
            LiveId = liveData.LiveId;
            CommunityName = liveData.BroadcasterName;
            if (liveData.ThumbnailUrl != null)
            {
                CommunityThumbnail = liveData.ThumbnailUrl;
            }
            else
            {
                CommunityThumbnail = liveData.PictureUrl;
            }

            CommunityGlobalId = liveData.BroadcasterId;
            CommunityType = liveData.ProviderType;

            LiveTitle = liveData.Title;
            ViewCounter = liveData.ViewCount;
            CommentCount = liveData.CommentCount;
            OpenTime = liveData.OpenTime;
            StartTime = liveData.StartTime;
            EndTime = liveData.EndTime;
            IsTimeshiftEnabled = liveData.TimeshiftEnabled;
            IsCommunityMemberOnly = liveData.IsMemberOnly;

            Label = LiveTitle;
            AddImageUrl(CommunityThumbnail);

            Description = $"来場者:{ViewCounter} コメ:{CommentCount}";

            if (StartTime > DateTimeOffset.Now)
            {
                // 予約
                DurationText = $" 開始予定: {StartTime.LocalDateTime.ToString("g")}";
            }
            else if (EndTime > DateTimeOffset.Now)
            {
                var duration = DateTimeOffset.Now - StartTime;
                // 放送中
                if (duration.Hours > 0)
                {
                    DurationText = $"{duration.Hours}時間 {duration.Minutes}分 経過";
                }
                else
                {
                    DurationText = $"{duration.Minutes}分 経過";
                }
            }
            else
            {
                var duration = EndTime - StartTime;
                // 終了
                if (duration.Hours > 0)
                {
                    DurationText = $"{EndTime.LocalDateTime.ToString("g")} 終了（{duration.Hours}時間 {duration.Minutes}分）";
                }
                else
                {
                    DurationText = $"{EndTime.LocalDateTime.ToString("g")} 終了（{duration.Minutes}分）";
                }
            }

            OptionText = DurationText;
        }


        private void ResetElements()
        {
            Elements.Clear();
            
            if (DateTimeOffset.Now < OpenTime)
            {
                Elements.Add(LiveContentElement.Status_Pending);
            }
            else if (OpenTime < DateTimeOffset.Now && DateTimeOffset.Now < StartTime)
            {
                Elements.Add(LiveContentElement.Status_Open);
            }
            else if (StartTime < DateTimeOffset.Now && DateTimeOffset.Now < EndTime)
            {
                Elements.Add(LiveContentElement.Status_Start);
            }
            else
            {
                Elements.Add(LiveContentElement.Status_Closed);
            }

            switch (CommunityType)
            {
                case CommunityType.Official:
                    Elements.Add(LiveContentElement.Provider_Official);
                    break;
                case CommunityType.Community:
                    Elements.Add(LiveContentElement.Provider_Community);
                    break;
                case CommunityType.Channel:
                    Elements.Add(LiveContentElement.Provider_Channel);
                    break;
                default:
                    break;
            }

           
            if (IsCommunityMemberOnly)
            {
                Elements.Add(LiveContentElement.MemberOnly);
            }

            if (Reservation != null)
            {
                if (Reservation.IsCanWatch && Elements.Any(x => x == LiveContentElement.Status_Closed))
                {
                    Elements.Add(LiveContentElement.Timeshift_Watch);
                }
                else if (Reservation.IsOutDated)
                {
                    Elements.Add(LiveContentElement.Timeshift_OutDated);
                }
                else
                {
                    Elements.Add(LiveContentElement.Timeshift_Preserved);
                }
            }
            else if (IsTimeshiftEnabled)
            {
                Elements.Add(LiveContentElement.Timeshift_Enable);
            }
        }

    }



    public enum LiveContentElement 
    {
        Provider_Community,
        Provider_Channel,
        Provider_Official,

        Status_Pending,
        Status_Open,
        Status_Start,
        Status_Closed,

        Timeshift_Enable,
        Timeshift_Preserved,
        Timeshift_OutDated,
        Timeshift_Watch,

        MemberOnly, 
    }
}
