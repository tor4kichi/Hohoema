using Mntone.Nico2.Mylist;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace Hohoema.Models.Repository.Niconico.Mylist
{
    public static class MylistGroupIconTypeColorExtension
    {
        public static Color ToColor(this MylistGroupIconType iconType) => iconType.ToInfrastructureIconType().ToColor();
    }
}
