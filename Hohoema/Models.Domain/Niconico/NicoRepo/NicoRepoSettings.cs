using Hohoema.Models.Infrastructure;
using Hohoema.Presentation.ViewModels;
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
            DisplayNicoRepoItemTopics = Read(new List<NicoRepoItemTopic>(), nameof(DisplayNicoRepoItemTopics));
        }

        private List<NicoRepoItemTopic> _DisplayNicoRepoItemTopics;
        public List<NicoRepoItemTopic> DisplayNicoRepoItemTopics
        {
            get { return _DisplayNicoRepoItemTopics; }
            set { SetProperty(ref _DisplayNicoRepoItemTopics, value); }
        }        
    }
}
