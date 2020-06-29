using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.NicoRepo
{
    public class NicoRepoSettingsRepository : FlagsRepositoryBase
    {
        static readonly NicoRepoItemTopic[] DefaultDisplayNicoRepoItemTopics = Enum.GetValues(typeof(NicoRepoItemTopic)).Cast<NicoRepoItemTopic>().ToArray();
        public NicoRepoItemTopic[] DisplayNicoRepoItemTopics
        {
            get => Read(DefaultDisplayNicoRepoItemTopics);
            set => Save(value ?? new NicoRepoItemTopic[0]);
        }
    }
}
