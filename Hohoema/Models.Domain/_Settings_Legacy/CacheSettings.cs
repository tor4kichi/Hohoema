using Hohoema.Models.Domain.Player.Video;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Legacy
{
	[DataContract]
	public class CacheSettings : SettingsBase
	{
		public CacheSettings()
		{
			IsEnableCache = false;
			IsUserAcceptedCache = false;
			IsAutoCacheOnPlayEnable = false;
			CacheOnPlayTagConditions = new ObservableCollection<TagCondition>();
		}

		[IgnoreDataMember]
		public bool CanDownload => IsUserAcceptedCache && IsEnableCache;


		private bool _IsEnableCache;

		[DataMember]
		public bool IsEnableCache
		{
			get { return _IsEnableCache; }
			set { SetProperty(ref _IsEnableCache, value); }
		}

		private bool _IsUserAcceptRegalNotice;

		[DataMember]
		public bool IsUserAcceptedCache
		{
			get { return _IsUserAcceptRegalNotice; }
			set { SetProperty(ref _IsUserAcceptRegalNotice, value); }
		}
		

		private bool _IsAutoCacheOnPlayEnable;

		[DataMember]
		public bool IsAutoCacheOnPlayEnable
		{
			get { return _IsAutoCacheOnPlayEnable; }
			set { SetProperty(ref _IsAutoCacheOnPlayEnable, value); }
		}


		[DataMember]
		public ObservableCollection<TagCondition> CacheOnPlayTagConditions { get; private set; }



        private NicoVideoQuality_Legacy _DefaultCacheQuality = NicoVideoQuality_Legacy.Dmc_Midium;

        [DataMember]
        public NicoVideoQuality_Legacy DefaultCacheQuality
        {
            get { return _DefaultCacheQuality; }
            set { SetProperty(ref _DefaultCacheQuality, value); }
        }





        private bool _IsAllowDownloadOnMeteredNetwork = false;

        [DataMember]
        public bool IsAllowDownloadOnMeteredNetwork
        {
            get { return _IsAllowDownloadOnMeteredNetwork; }
            set { SetProperty(ref _IsAllowDownloadOnMeteredNetwork, value); }
        }



        private NicoVideoQuality_Legacy _DefaultCacheQualityOnMeteredNetwork = NicoVideoQuality_Legacy.Dmc_Mobile;

        [DataMember]
        public NicoVideoQuality_Legacy DefaultCacheQualityOnMeteredNetwork
        {
            get { return _DefaultCacheQualityOnMeteredNetwork; }
            set { SetProperty(ref _DefaultCacheQualityOnMeteredNetwork, value); }
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

		private NicoVideoQuality_Legacy? _Quality;

		[DataMember]
		public NicoVideoQuality_Legacy? Quality
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
