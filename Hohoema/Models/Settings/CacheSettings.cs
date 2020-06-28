using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models
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



        private NicoVideoQuality _DefaultCacheQuality = NicoVideoQuality.Dmc_Midium;

        [DataMember]
        public NicoVideoQuality DefaultCacheQuality
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



        private NicoVideoQuality _DefaultCacheQualityOnMeteredNetwork = NicoVideoQuality.Dmc_Mobile;

        [DataMember]
        public NicoVideoQuality DefaultCacheQualityOnMeteredNetwork
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
