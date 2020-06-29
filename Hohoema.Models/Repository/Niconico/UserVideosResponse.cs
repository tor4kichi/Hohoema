using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.Niconico
{
    public sealed class UserVideosResponse
    {
        public uint UserId { get; set; }
        public string UserName { get; set; }

        public List<Database.NicoVideo> Items { get; set; }
    }
}
