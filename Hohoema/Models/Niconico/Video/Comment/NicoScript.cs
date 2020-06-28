using Hohoema.Models.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Niconico.Video
{
    public abstract class NicoScirptBase
    {
        public NicoScirptBase(string type)
        {
            Type = type;
        }

        public string Type { get; private set; }
        public TimeSpan BeginTime { get; set; }
        public TimeSpan? EndTime { get; set; }

        public uint? _BeginVPos;
        public uint BeginVPos => (_BeginVPos ?? (_BeginVPos = (uint)(BeginTime.TotalMilliseconds * 0.1))).Value;

        public uint? _EndVPos;
        public uint EndVPos => EndTime.HasValue ? (_EndVPos ?? (_EndVPos = (uint)(EndTime.Value.TotalMilliseconds * 0.1))).Value : uint.MaxValue;
    }

    public sealed class NicoScript : NicoScirptBase
    {
        public NicoScript(string type)
            : base(type)
        {
        }

        public Action ScriptEnabling { get; set; }
        public Action ScriptDisabling { get; set; }
    }

    public enum ReplaceNicoScriptRange
    {
        単,
        全,
    }

    public enum ReplaceNicoScriptTarget
    {
        コメ,
        全,
        投コメ,
        含まない,
        含む,
    }

    public enum ReplaceNicoScriptCondition
    {
        部分一致,
        完全一致,
    }

    public sealed class ReplaceNicoScript : NicoScirptBase
    {
        public ReplaceNicoScript(string type)
            : base(type)
        {
        }

        public string Commands { get; set; }

        public string TargetText { get; set; }
        public string ReplaceText { get; set; }
        public ReplaceNicoScriptRange Range { get; set; }
        public ReplaceNicoScriptTarget Target { get; set; }
        public ReplaceNicoScriptCondition Condition { get; set; }

    }

    public sealed class DefaultCommandNicoScript : NicoScirptBase
    {
        public DefaultCommandNicoScript(string type)
            : base(type)
        {
        }

        public string[] Command { get; set; }

        List<Action<Comment>> _CommandActions;

        public void ApplyCommand(Comment commentVM)
        {
            if (_CommandActions == null)
            {
                _CommandActions = MailToCommandHelper.MakeCommandActions(Command).ToList();
            }

            foreach (var action in _CommandActions)
            {
                action(commentVM);
            }
        }


        public const float fontSize_mid = 1.0f;
        public const float fontSize_small = 0.75f;
        public const float fontSize_big = 1.25f;

        
    }


    public static class MailToCommandHelper
    {
        public static IEnumerable<Action<Comment>> MakeCommandActions(IEnumerable<string> commands)
        {
            foreach (var command in commands)
            {
                switch (command)
                {
                    case "small":
                        yield return c => c.SizeMode = CommentSizeMode.Small;
                        break;
                    case "big":
                        yield return c => c.SizeMode = CommentSizeMode.Small;
                        break;
                    case "medium":
                        yield return c => c.SizeMode = CommentSizeMode.Normal;
                        break;
                    case "ue":
                        yield return c => c.DisplayMode = CommentDisplayMode.Top;
                        break;
                    case "shita":
                        yield return c => c.DisplayMode = CommentDisplayMode.Bottom;
                        break;
                    case "naka":
                        yield return c => c.DisplayMode = CommentDisplayMode.Center;
                        break;
                    case "white":
                        yield return c => c.Color = ColorExtention.HexStringToColor("FFFFFF");
                        break;
                    case "red":
                        yield return c => c.Color = ColorExtention.HexStringToColor("FF0000");
                        break;
                    case "pink":
                        yield return c => c.Color = ColorExtention.HexStringToColor("FF8080");
                        break;
                    case "orange":
                        yield return c => c.Color = ColorExtention.HexStringToColor("FFC000");
                        break;
                    case "yellow":
                        yield return c => c.Color = ColorExtention.HexStringToColor("FFFF00");
                        break;
                    case "green":
                        yield return c => c.Color = ColorExtention.HexStringToColor("00FF00");
                        break;
                    case "cyan":
                        yield return c => c.Color = ColorExtention.HexStringToColor("00FFFF");
                        break;
                    case "blue":
                        yield return c => c.Color = ColorExtention.HexStringToColor("0000FF");
                        break;
                    case "purple":
                        yield return c => c.Color = ColorExtention.HexStringToColor("C000FF");
                        break;
                    case "black":
                        yield return c => c.Color = ColorExtention.HexStringToColor("000000");
                        break;
                    case "white2":
                        yield return c => c.Color = ColorExtention.HexStringToColor("CCCC99");
                        break;
                    case "niconicowhite":
                        yield return c => c.Color = ColorExtention.HexStringToColor("CCCC99");
                        break;
                    case "red2":
                        yield return c => c.Color = ColorExtention.HexStringToColor("CC0033");
                        break;
                    case "truered":
                        yield return c => c.Color = ColorExtention.HexStringToColor("CC0033");
                        break;
                    case "pink2":
                        yield return c => c.Color = ColorExtention.HexStringToColor("FF33CC");
                        break;
                    case "orange2":
                        yield return c => c.Color = ColorExtention.HexStringToColor("FF6600");
                        break;
                    case "passionorange":
                        yield return c => c.Color = ColorExtention.HexStringToColor("FF6600");
                        break;
                    case "yellow2":
                        yield return c => c.Color = ColorExtention.HexStringToColor("999900");
                        break;
                    case "madyellow":
                        yield return c => c.Color = ColorExtention.HexStringToColor("999900");
                        break;
                    case "green2":
                        yield return c => c.Color = ColorExtention.HexStringToColor("00CC66");
                        break;
                    case "elementalgreen":
                        yield return c => c.Color = ColorExtention.HexStringToColor("00CC66");
                        break;
                    case "cyan2":
                        yield return c => c.Color = ColorExtention.HexStringToColor("00CCCC");
                        break;
                    case "blue2":
                        yield return c => c.Color = ColorExtention.HexStringToColor("3399FF");
                        break;
                    case "marineblue":
                        yield return c => c.Color = ColorExtention.HexStringToColor("3399FF");
                        break;
                    case "purple2":
                        yield return c => c.Color = ColorExtention.HexStringToColor("6633CC");
                        break;
                    case "nobleviolet":
                        yield return c => c.Color = ColorExtention.HexStringToColor("6633CC");
                        break;
                    case "black2":
                        yield return c => c.Color = ColorExtention.HexStringToColor("666666");
                        break;
                    case "full":
                        break;
                    case "_184":
                        yield return c => c.IsAnonimity = true;
                        break;
                    case "invisible":
                        yield return c => c.IsInvisible = true;
                        break;
                    case "all":
                        // Note": 事前に判定しているのでここでは評価しない
                        break;
                    case "from_button":
                        break;
                    case "is_button":
                        break;
                    case "_live":

                        break;
                    default:
                        if (command.StartsWith("#"))
                        {
                            var color = ColorExtention.HexStringToColor(command.Remove(0, 1));
                            yield return c => c.Color = color;
                        }
                        break;
                }
            }
        }
    }
}
