using Mntone.Nico2.Videos.Thumbnail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Interfaces
{
    public interface IVideoContent : INiconicoContent
    {
        string OwnerUserId { get; }
        string OwnerUserName { get; }
        UserType OwnerUserType { get; }
    }
}
