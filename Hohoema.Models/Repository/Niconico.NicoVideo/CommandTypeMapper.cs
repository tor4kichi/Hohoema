using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.Niconico.NicoVideo
{
    internal static class CommandTypeMapper
    {
        public static CommandType ToModelCommandType(this Mntone.Nico2.Videos.Comment.CommandType commandType)
        {
            return commandType switch
            {
                Mntone.Nico2.Videos.Comment.CommandType.small => CommandType.small,
                Mntone.Nico2.Videos.Comment.CommandType.big => CommandType.big,
                Mntone.Nico2.Videos.Comment.CommandType.medium => CommandType.medium,
                Mntone.Nico2.Videos.Comment.CommandType.ue => CommandType.ue,
                Mntone.Nico2.Videos.Comment.CommandType.shita => CommandType.shita,
                Mntone.Nico2.Videos.Comment.CommandType.naka => CommandType.naka,
                Mntone.Nico2.Videos.Comment.CommandType.white => CommandType.white,
                Mntone.Nico2.Videos.Comment.CommandType.red => CommandType.red,
                Mntone.Nico2.Videos.Comment.CommandType.pink => CommandType.pink,
                Mntone.Nico2.Videos.Comment.CommandType.orange => CommandType.orange,
                Mntone.Nico2.Videos.Comment.CommandType.yellow => CommandType.yellow,
                Mntone.Nico2.Videos.Comment.CommandType.green => CommandType.green,
                Mntone.Nico2.Videos.Comment.CommandType.cyan => CommandType.cyan,
                Mntone.Nico2.Videos.Comment.CommandType.blue => CommandType.blue,
                Mntone.Nico2.Videos.Comment.CommandType.purple => CommandType.purple,
                Mntone.Nico2.Videos.Comment.CommandType.black => CommandType.black,
                Mntone.Nico2.Videos.Comment.CommandType.white2 => CommandType.white,
                Mntone.Nico2.Videos.Comment.CommandType.niconicowhite => CommandType.niconicowhite,
                Mntone.Nico2.Videos.Comment.CommandType.red2 => CommandType.red,
                Mntone.Nico2.Videos.Comment.CommandType.truered => CommandType.truered,
                Mntone.Nico2.Videos.Comment.CommandType.pink2 => CommandType.pink2,
                Mntone.Nico2.Videos.Comment.CommandType.orange2 => CommandType.orange2,
                Mntone.Nico2.Videos.Comment.CommandType.passionorange => CommandType.passionorange,
                Mntone.Nico2.Videos.Comment.CommandType.yellow2 => CommandType.yellow2,
                Mntone.Nico2.Videos.Comment.CommandType.madyellow => CommandType.madyellow,
                Mntone.Nico2.Videos.Comment.CommandType.green2 => CommandType.green2,
                Mntone.Nico2.Videos.Comment.CommandType.elementalgreen => CommandType.elementalgreen,
                Mntone.Nico2.Videos.Comment.CommandType.cyan2 => CommandType.cyan2,
                Mntone.Nico2.Videos.Comment.CommandType.blue2 => CommandType.blue2,
                Mntone.Nico2.Videos.Comment.CommandType.marineblue => CommandType.marineblue,
                Mntone.Nico2.Videos.Comment.CommandType.purple2 => CommandType.purple2,
                Mntone.Nico2.Videos.Comment.CommandType.nobleviolet => CommandType.nobleviolet,
                Mntone.Nico2.Videos.Comment.CommandType.black2 => CommandType.black2,
                Mntone.Nico2.Videos.Comment.CommandType.full => CommandType.full,
                Mntone.Nico2.Videos.Comment.CommandType._184 => CommandType._184,
                Mntone.Nico2.Videos.Comment.CommandType.invisible => CommandType.invisible,
                Mntone.Nico2.Videos.Comment.CommandType.all => CommandType.all,
                Mntone.Nico2.Videos.Comment.CommandType.from_button => CommandType.from_button,
                Mntone.Nico2.Videos.Comment.CommandType.is_button => CommandType.is_button,
                Mntone.Nico2.Videos.Comment.CommandType._live => CommandType._live,
                _ => throw new NotSupportedException(commandType.ToString())
            };
        }


        public static Mntone.Nico2.Videos.Comment.CommandType ToInfrastructureCommandType(this CommandType commandType)
        {
            return commandType switch
            {
                CommandType.small => Mntone.Nico2.Videos.Comment.CommandType.small,
                CommandType.big => Mntone.Nico2.Videos.Comment.CommandType.big,
                CommandType.medium => Mntone.Nico2.Videos.Comment.CommandType.medium,
                CommandType.ue => Mntone.Nico2.Videos.Comment.CommandType.ue,
                CommandType.shita => Mntone.Nico2.Videos.Comment.CommandType.shita,
                CommandType.naka => Mntone.Nico2.Videos.Comment.CommandType.naka,
                CommandType.white => Mntone.Nico2.Videos.Comment.CommandType.white,
                CommandType.red => Mntone.Nico2.Videos.Comment.CommandType.red,
                CommandType.pink => Mntone.Nico2.Videos.Comment.CommandType.pink,
                CommandType.orange => Mntone.Nico2.Videos.Comment.CommandType.orange,
                CommandType.yellow => Mntone.Nico2.Videos.Comment.CommandType.yellow,
                CommandType.green => Mntone.Nico2.Videos.Comment.CommandType.green,
                CommandType.cyan => Mntone.Nico2.Videos.Comment.CommandType.cyan,
                CommandType.blue => Mntone.Nico2.Videos.Comment.CommandType.blue,
                CommandType.purple => Mntone.Nico2.Videos.Comment.CommandType.purple,
                CommandType.black => Mntone.Nico2.Videos.Comment.CommandType.black,
                CommandType.white2 => Mntone.Nico2.Videos.Comment.CommandType.white,
                CommandType.niconicowhite => Mntone.Nico2.Videos.Comment.CommandType.niconicowhite,
                CommandType.red2 => Mntone.Nico2.Videos.Comment.CommandType.red,
                CommandType.truered => Mntone.Nico2.Videos.Comment.CommandType.truered,
                CommandType.pink2 => Mntone.Nico2.Videos.Comment.CommandType.pink2,
                CommandType.orange2 => Mntone.Nico2.Videos.Comment.CommandType.orange2,
                CommandType.passionorange => Mntone.Nico2.Videos.Comment.CommandType.passionorange,
                CommandType.yellow2 => Mntone.Nico2.Videos.Comment.CommandType.yellow2,
                CommandType.madyellow => Mntone.Nico2.Videos.Comment.CommandType.madyellow,
                CommandType.green2 => Mntone.Nico2.Videos.Comment.CommandType.green2,
                CommandType.elementalgreen => Mntone.Nico2.Videos.Comment.CommandType.elementalgreen,
                CommandType.cyan2 => Mntone.Nico2.Videos.Comment.CommandType.cyan2,
                CommandType.blue2 => Mntone.Nico2.Videos.Comment.CommandType.blue2,
                CommandType.marineblue => Mntone.Nico2.Videos.Comment.CommandType.marineblue,
                CommandType.purple2 => Mntone.Nico2.Videos.Comment.CommandType.purple2,
                CommandType.nobleviolet => Mntone.Nico2.Videos.Comment.CommandType.nobleviolet,
                CommandType.black2 => Mntone.Nico2.Videos.Comment.CommandType.black2,
                CommandType.full => Mntone.Nico2.Videos.Comment.CommandType.full,
                CommandType._184 => Mntone.Nico2.Videos.Comment.CommandType._184,
                CommandType.invisible => Mntone.Nico2.Videos.Comment.CommandType.invisible,
                CommandType.all => Mntone.Nico2.Videos.Comment.CommandType.all,
                CommandType.from_button => Mntone.Nico2.Videos.Comment.CommandType.from_button,
                CommandType.is_button => Mntone.Nico2.Videos.Comment.CommandType.is_button,
                CommandType._live => Mntone.Nico2.Videos.Comment.CommandType._live,
                _ => throw new NotSupportedException(commandType.ToString())
            };
        }
    }
}
