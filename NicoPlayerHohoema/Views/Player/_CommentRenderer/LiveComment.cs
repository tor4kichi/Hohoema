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

        private string _UserName;
        public string UserName
        {
            get { return _UserName; }
            set { SetProperty(ref _UserName, value); }
        }

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

    }
}
