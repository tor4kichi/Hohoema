using Hohoema.Models.Infrastructure;
using Hohoema.Presentation.ViewModels;
using NiconicoToolkit.NicoRepo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Niconico.NicoRepo
{
    public class NicoRepoSettings : FlagsRepositoryBase
    {
        public NicoRepoSettings()
        {
            DisplayNicoRepoMuteContextTriggers = Read(new List<NicoRepoMuteContextTrigger>(), nameof(DisplayNicoRepoMuteContextTriggers));
        }

        private List<NicoRepoMuteContextTrigger> _DisplayNicoRepoMuteContextTriggers;
        public List<NicoRepoMuteContextTrigger> DisplayNicoRepoMuteContextTriggers
        {
            get { return _DisplayNicoRepoMuteContextTriggers; }
            set { SetProperty(ref _DisplayNicoRepoMuteContextTriggers, value); }
        }        
    }
}
