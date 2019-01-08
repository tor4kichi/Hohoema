using NicoPlayerHohoema.Models.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models.Niconico.Video
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
                _CommandActions = MakeCommandActions(Command);
            }

            foreach (var action in _CommandActions)
            {
                action(commentVM);
            }
        }


        public const float fontSize_mid = 1.0f;
        public const float fontSize_small = 0.75f;
        public const float fontSize_big = 1.25f;

        public static List<Action<Comment>> MakeCommandActions(string[] commands)
        {
            List<Action<Comment>> actions = new List<Action<Comment>>();
            foreach (var command in commands)
            {
                switch (command)
                {
                    case "small":
                        actions.Add(c => c.SizeMode = CommentSizeMode.Small);
                        break;
                    case "big":
                        actions.Add(c => c.SizeMode = CommentSizeMode.Small);
                        break;
                    case "medium":
                        actions.Add(c => c.SizeMode = CommentSizeMode.Normal);
                        break;
                    case "ue":
                        actions.Add(c => c.DisplayMode = CommentDisplayMode.Top);
                        break;
                    case "shita":
                        actions.Add(c => c.DisplayMode = CommentDisplayMode.Bottom);
                        break;
                    case "naka":
                        actions.Add(c => c.DisplayMode = CommentDisplayMode.Center);
                        break;
                    case "white":
                        actions.Add(c => c.Color = ColorExtention.HexStringToColor("FFFFFF"));
                        break;
                    case "red":
                        actions.Add(c => c.Color = ColorExtention.HexStringToColor("FF0000"));
                        break;
                    case "pink":
                        actions.Add(c => c.Color = ColorExtention.HexStringToColor("FF8080"));
                        break;
                    case "orange":
                        actions.Add(c => c.Color = ColorExtention.HexStringToColor("FFC000"));
                        break;
                    case "yellow":
                        actions.Add(c => c.Color = ColorExtention.HexStringToColor("FFFF00"));
                        break;
                    case "green":
                        actions.Add(c => c.Color = ColorExtention.HexStringToColor("00FF00"));
                        break;
                    case "cyan":
                        actions.Add(c => c.Color = ColorExtention.HexStringToColor("00FFFF"));
                        break;
                    case "blue":
                        actions.Add(c => c.Color = ColorExtention.HexStringToColor("0000FF"));
                        break;
                    case "purple":
                        actions.Add(c => c.Color = ColorExtention.HexStringToColor("C000FF"));
                        break;
                    case "black":
                        actions.Add(c => c.Color = ColorExtention.HexStringToColor("000000"));
                        break;
                    case "white2":
                        actions.Add(c => c.Color = ColorExtention.HexStringToColor("CCCC99"));
                        break;
                    case "niconicowhite":
                        actions.Add(c => c.Color = ColorExtention.HexStringToColor("CCCC99"));
                        break;
                    case "red2":
                        actions.Add(c => c.Color = ColorExtention.HexStringToColor("CC0033"));
                        break;
                    case "truered":
                        actions.Add(c => c.Color = ColorExtention.HexStringToColor("CC0033"));
                        break;
                    case "pink2":
                        actions.Add(c => c.Color = ColorExtention.HexStringToColor("FF33CC"));
                        break;
                    case "orange2":
                        actions.Add(c => c.Color = ColorExtention.HexStringToColor("FF6600"));
                        break;
                    case "passionorange":
                        actions.Add(c => c.Color = ColorExtention.HexStringToColor("FF6600"));
                        break;
                    case "yellow2":
                        actions.Add(c => c.Color = ColorExtention.HexStringToColor("999900"));
                        break;
                    case "madyellow":
                        actions.Add(c => c.Color = ColorExtention.HexStringToColor("999900"));
                        break;
                    case "green2":
                        actions.Add(c => c.Color = ColorExtention.HexStringToColor("00CC66"));
                        break;
                    case "elementalgreen":
                        actions.Add(c => c.Color = ColorExtention.HexStringToColor("00CC66"));
                        break;
                    case "cyan2":
                        actions.Add(c => c.Color = ColorExtention.HexStringToColor("00CCCC"));
                        break;
                    case "blue2":
                        actions.Add(c => c.Color = ColorExtention.HexStringToColor("3399FF"));
                        break;
                    case "marineblue":
                        actions.Add(c => c.Color = ColorExtention.HexStringToColor("3399FF"));
                        break;
                    case "purple2":
                        actions.Add(c => c.Color = ColorExtention.HexStringToColor("6633CC"));
                        break;
                    case "nobleviolet":
                        actions.Add(c => c.Color = ColorExtention.HexStringToColor("6633CC"));
                        break;
                    case "black2":
                        actions.Add(c => c.Color = ColorExtention.HexStringToColor("666666"));
                        break;
                    case "full":
                        break;
                    case "_184":
                        actions.Add(c => c.IsAnonimity = true);
                        break;
                    case "invisible":
                        actions.Add(c => c.IsInvisible = true);
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
                            actions.Add(c => c.Color = color);
                        }
                        break;
                }
            }

            return actions;
        }
    }
}
