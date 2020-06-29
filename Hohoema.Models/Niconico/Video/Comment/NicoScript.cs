using Hohoema.Models.Helpers;
using Hohoema.Models.Repository.Niconico.NicoVideo.Comment;
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
        
    }

}
