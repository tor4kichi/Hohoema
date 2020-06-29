using Mntone.Nico2.Mylist;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.Niconico.Mylist
{
    internal static class MylistGroupIconTypeMapper
    {
        public static MylistGroupIconType ToModelIconType(this IconType iconType) => iconType switch
        {
            IconType.Default => MylistGroupIconType.Default,
            IconType.Cyan => MylistGroupIconType.Cyan,
            IconType.SmokeWhite => MylistGroupIconType.SmokeWhite,
            IconType.Dark => MylistGroupIconType.Dark,
            IconType.Red => MylistGroupIconType.Red,
            IconType.Orenge => MylistGroupIconType.Orenge,
            IconType.Green => MylistGroupIconType.Green,
            IconType.SkyBlue => MylistGroupIconType.SkyBlue,
            IconType.Blue => MylistGroupIconType.Blue,
            IconType.Purple => MylistGroupIconType.Purple,
            _ => throw new NotSupportedException(iconType.ToString()),
        };


        public static IconType ToInfrastructureIconType(this MylistGroupIconType iconType) => iconType switch
        {
            MylistGroupIconType.Default => IconType.Default,
            MylistGroupIconType.Cyan => IconType.Cyan,
            MylistGroupIconType.SmokeWhite => IconType.SmokeWhite,
            MylistGroupIconType.Dark => IconType.Dark,
            MylistGroupIconType.Red => IconType.Red,
            MylistGroupIconType.Orenge => IconType.Orenge,
            MylistGroupIconType.Green => IconType.Green,
            MylistGroupIconType.SkyBlue => IconType.SkyBlue,
            MylistGroupIconType.Blue => IconType.Blue,
            MylistGroupIconType.Purple => IconType.Purple,
            _ => throw new NotSupportedException(iconType.ToString()),
        };
    }
}
