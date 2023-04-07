using Hohoema.Infra;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Niconico.Video
{
	public class VideoCacheSettings_Legacy : FlagsRepositoryBase
	{
		public VideoCacheSettings_Legacy()
		{
			_IsEnableCache = Read(false, nameof(IsEnableCache));
			_IsUserAcceptedCache = Read(false, nameof(IsUserAcceptedCache));
			_DefaultCacheQuality = Read(NicoVideoQuality.Midium, nameof(DefaultCacheQuality));

			_IsAutoCacheOnPlayEnable = Read(false, nameof(IsAutoCacheOnPlayEnable));
			_IsAllowDownloadOnMeteredNetwork = Read(false, nameof(IsAllowDownloadOnMeteredNetwork));
			_CacheQualityOnMeteredNetwork = Read(NicoVideoQuality.Mobile, nameof(CacheQualityOnMeteredNetwork));
		}

		public bool CanDownload => IsUserAcceptedCache && IsEnableCache;


		private bool _IsEnableCache;

		public bool IsEnableCache
		{
			get { return _IsEnableCache; }
			set { SetProperty(ref _IsEnableCache, value); }
		}

		private bool _IsUserAcceptedCache;

		public bool IsUserAcceptedCache
		{
			get { return _IsUserAcceptedCache; }
			set { SetProperty(ref _IsUserAcceptedCache, value); }
		}
		

		private bool _IsAutoCacheOnPlayEnable;

		public bool IsAutoCacheOnPlayEnable
		{
			get { return _IsAutoCacheOnPlayEnable; }
			set { SetProperty(ref _IsAutoCacheOnPlayEnable, value); }
		}


        private NicoVideoQuality _DefaultCacheQuality = NicoVideoQuality.Midium;

        public NicoVideoQuality DefaultCacheQuality
        {
            get { return _DefaultCacheQuality; }
            set { SetProperty(ref _DefaultCacheQuality, value); }
        }





        private bool _IsAllowDownloadOnMeteredNetwork = false;

        public bool IsAllowDownloadOnMeteredNetwork
        {
            get { return _IsAllowDownloadOnMeteredNetwork; }
            set { SetProperty(ref _IsAllowDownloadOnMeteredNetwork, value); }
        }



        private NicoVideoQuality _CacheQualityOnMeteredNetwork = NicoVideoQuality.Mobile;

        public NicoVideoQuality CacheQualityOnMeteredNetwork
        {
            get { return _CacheQualityOnMeteredNetwork; }
            set { SetProperty(ref _CacheQualityOnMeteredNetwork, value); }
        }
    }

	[DataContract]
	public class TagCondition : ObservableObject
	{
		public TagCondition()
		{
			Label = "";
			IncludeTags = new ObservableCollection<string>();
			ExcludeTags = new ObservableCollection<string>();
			Quality = null;
		}

		private string _Label;

		[DataMember]
		public string Label
		{
			get { return _Label; }
			set { SetProperty(ref _Label, value); }
		}

		private NicoVideoQuality? _Quality;

		[DataMember]
		public NicoVideoQuality? Quality
		{
			get { return _Quality; }
			set { SetProperty(ref _Quality, value); }
		}

		[DataMember]
		public ObservableCollection<string> IncludeTags { get; private set; }
		[DataMember]
		public ObservableCollection<string> ExcludeTags { get; private set; }
	}

	
}
