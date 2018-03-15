using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NicoPlayerHohoema.Models;

namespace NicoPlayerHohoema.Views
{
    public sealed class LiveComment : Comment
    {
        static Dictionary<string, string> _UserIdToKotehan = new Dictionary<string, string>();

        public static void AddOrUpdateKotehan(string userId, string kotehan)
        {
            if (_UserIdToKotehan.ContainsKey(userId))
            {
                _UserIdToKotehan[userId] = kotehan;
            }
            else
            {
                _UserIdToKotehan.Add(userId, kotehan);
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"コテハン追加： {kotehan} (id:{userId})");
#endif
            }
        }

        public static void ClearAllKotehan()
        {
            _UserIdToKotehan.Clear();
        }




        private string _UserName;
        public string UserName
        {
            get { return _UserName; }
            set { SetProperty(ref _UserName, value); }
        }

        public string Kotehan => _UserIdToKotehan.TryGetValue(UserId, out var kotehan) ? kotehan : null;

        private string _IconUrl;
        public string IconUrl
        {
            get { return _IconUrl; }
            set { SetProperty(ref _IconUrl, value); }
        }
        public LiveComment(NGSettings ngsettings) 
            : base(ngsettings)
        {
        }

        public void RaiseKotehanChanged()
        {
            RaisePropertyChanged(nameof(Kotehan));
        }

    }
}
