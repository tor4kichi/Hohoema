using Hohoema.Models.Niconico.Video;
using Mntone.Nico2.Mylist;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.Niconico.NicoVideo.RelatedVideos
{
	public class RelatedVideosReponse
	{
        private NicoVideoResponse _res;

        public RelatedVideosReponse(NicoVideoResponse res)
        {
            _res = res;
        }

		public string Count => _res.Count;

		List<Videoinfo> _VideoItems;
		public List<Videoinfo> VideoItems => _VideoItems ??= _res.Video_info?.Select(x => new Videoinfo(x)).ToList();
		public string TotalCount => _res.Total_count;
		public string Status => _res.Status;

		public bool IsOK => Status == "ok";


		public class Videoinfo
		{
            private Mntone.Nico2.Mylist.Video_info _info;

            public Videoinfo(Mntone.Nico2.Mylist.Video_info info)
            {
                _info = info;
            }

			Video _Video;
			public Video Video => _Video ??= new Video(_info.Video);
			Thread _Thread;
			public Thread Thread => _Thread ??= new Thread(_info.Thread);
			Mylist _Mylist;
			public Mylist Mylist => _Mylist ??= new Mylist(_info.Mylist);
		}

		public class Video
		{
            private Mntone.Nico2.Mylist.Video _video;

            public Video(Mntone.Nico2.Mylist.Video video)
            {
                _video = video;
            }

			public string Id => _video.Id;
			public string IsDeleted => _video.Deleted;
			public string Title => _video.Title;

			TimeSpan? _LengthInSeconds;
			public TimeSpan LengthInSeconds => _LengthInSeconds ??= TimeSpan.FromSeconds(int.Parse(_video.Length_in_seconds));

			public string Thumbnail_url => _video.Thumbnail_url;

			DateTime? _UploadTime;
			public DateTime UploadTime => _UploadTime ??= DateTime.Parse(_video.Upload_time);

			DateTime? _FirstRetrieve;
			public DateTime FirstRetrieve => _FirstRetrieve ??= DateTime.Parse(_video.First_retrieve);

			int? _ViewCount;
			public int ViewCount => _ViewCount ??= int.Parse(_video.View_counter);

			int? _MylistCount;
			public int MylistCount => _MylistCount ??= int.Parse(_video.Mylist_counter);

			public string Option_flag_community => _video.Option_flag_community;
			public string Option_flag_nicowari => _video.Option_flag_nicowari;
			public string Option_flag_middle_thumbnail => _video.Option_flag_middle_thumbnail;

			int? _Width;
			public int Width => _Width ??= int.Parse(_video.Width);
			int? _Height;
			public int Height => _Height ??= int.Parse(_video.Height);

			public string PpvVideo => _video.Ppv_video;

			VideoProviderType? _ProviderType;
			public VideoProviderType ProviderType => _ProviderType ??= _video.Provider_type switch
            {
				"channel" => VideoProviderType.Channel,
                _ => VideoProviderType.User,
            };
		}

		public class Thread
		{
            private Mntone.Nico2.Mylist.Thread _thread;

            public Thread(Mntone.Nico2.Mylist.Thread thread)
            {
                _thread = thread;
            }

			public string Id => _thread.Id;

			int? _CommentCount;
			public int CommentCount => _CommentCount ??= int.Parse(_thread.Num_res);

			public string Summary => _thread.Summary;
		}

		public class Mylist
		{
            private Mntone.Nico2.Mylist.Mylist _mylist;

            public Mylist(Mntone.Nico2.Mylist.Mylist mylist)
            {
                _mylist = mylist;
            }

			public string ItemId => _mylist.Item_id;
			public string Description => _mylist.Description;


			DateTime? _UpdateTime;
			public DateTime UpdateTime => _UpdateTime ??= DateTime.Parse(_mylist.Update_time);

			DateTime? _CreateTime;
			public DateTime CreateTime => _CreateTime ??= DateTime.Parse(_mylist.Create_time);

		}
	}

}
