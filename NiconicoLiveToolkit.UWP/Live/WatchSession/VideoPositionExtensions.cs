using System;
using System.Collections.Generic;
using System.Text;

namespace NiconicoToolkit.Live.WatchSession
{
    // Note: 1 vpos = 10 milliseconds

    public static class VideoPositionExtensions
    {
        public static TimeSpan ToTimeSpan(this int vpos)
        {
            return TimeSpan.FromMilliseconds(vpos * 10);
        }

        public static TimeSpan ToTimeSpan(this uint vpos)
        {
            return TimeSpan.FromMilliseconds(vpos * 10);
        }

        public static int ToVideoPosition(this TimeSpan position)
        {
            return (int)((float)position.TotalMilliseconds * 0.1f);
        }

    }
}
