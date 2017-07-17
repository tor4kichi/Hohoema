using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
    public enum CommentOpacityKind
    {
        NoSukesuke,
        BitSukesuke,
        MoreSukesuke,
    }


    public static class CommentOpacityKindExtention
    {
        public static double ToOpacity(this CommentOpacityKind kind)
        {
            switch (kind)
            {
                case CommentOpacityKind.NoSukesuke:
                    return 1.0;
                case CommentOpacityKind.BitSukesuke:
                    return 0.7;
                case CommentOpacityKind.MoreSukesuke:
                    return 0.35;
                default:
                    return 1.0;
            }
        }
    }
}
