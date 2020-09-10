using Hohoema.Models.Infrastructure;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Niconico.Video
{
	public class VideoCacheSettings : FlagsRepositoryBase
	{
		public VideoCacheSettings()
		{
			_IsEnableCache = Read(false, nameof(IsEnableCache));
			_IsUserAcceptedCache = Read(false, nameof(IsUserAcceptedCache));
			_DefaultCacheQuality = Read(NicoVideoQuality.Dmc_Midium, nameof(DefaultCacheQuality));

			_IsAutoCacheOnPlayEnable = Read(false, nameof(IsAutoCacheOnPlayEnable));
			_IsAllowDownloadOnMeteredNetwork = Read(false, nameof(IsAllowDownloadOnMeteredNetwork));
			_CacheQualityOnMeteredNetwork = Read(NicoVideoQuality.Dmc_Mobile, nameof(CacheQualityOnMeteredNetwork));
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


        private NicoVideoQuality _DefaultCacheQuality = NicoVideoQuality.Dmc_Midium;

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



        private NicoVideoQuality _CacheQualityOnMeteredNetwork = NicoVideoQuality.Dmc_Mobile;

        public NicoVideoQuality CacheQualityOnMeteredNetwork
        {
            get { return _CacheQualityOnMeteredNetwork; }
            set { SetProperty(ref _CacheQualityOnMeteredNetwork, value); }
        }
    }

	[DataContract]
	public class TagCondition : BindableBase
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
