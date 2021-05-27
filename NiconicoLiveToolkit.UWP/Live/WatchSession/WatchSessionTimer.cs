using NeoSmart.AsyncLock;
using NiconicoToolkit.Live.WatchPageProp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace NiconicoToolkit.Live.WatchSession
{
    public sealed class WatchSessionTimer 
    {
        public static WatchSessionTimer CreateForLiveStreaming(Program liveProgram, bool chasePlay)
        {
            return new WatchSessionTimer(liveProgram, false, false);
        }

        public static WatchSessionTimer CreateForTimeshift(Program liveProgram)
        {
            return new WatchSessionTimer(liveProgram, true, false);
        }


        public static readonly TimeSpan JapanTimeZoneOffset = +TimeSpan.FromHours(9);
        private readonly LiveWatchPageDataProp _prop;
        private readonly Program _liveProgram;
        private readonly bool _isTimeshift;
        private readonly bool _chasePlay;


        // OpenTime 開場時刻
        // StartTime 開始時刻
        // EndTime 終了時刻
        private WatchSessionTimer(Program liveProgram, bool isTimeshift, bool chasePlay)
        {
            _liveProgram = liveProgram;
            _isTimeshift = isTimeshift;
            _chasePlay = chasePlay;

            if (!_isTimeshift)
            {
                _updateMethod = UpdateLiveElapsedTimeAsync_LiveStreaming;
                _ToOpenTimeOffset = DateTimeOffset.Now.ToOffset(JapanTimeZoneOffset) - OpenTime;
            }
            else
            {
                _updateMethod = UpdateLiveElapsedTimeAsync_Timeshift;
                _ToOpenTimeOffset = JapanTimeZoneOffset;
                // TODO: チェイスプレイを有効にしていた場合 PlaybackHeadPosition が必要
            }
        }

        DateTimeOffset? _OpenTime;
        public DateTimeOffset OpenTime => _OpenTime ??= DateTimeOffset.FromUnixTimeSeconds(_liveProgram.OpenTime);
        DateTimeOffset? _StartTime;
        public DateTimeOffset StartTime => _StartTime ??= DateTimeOffset.FromUnixTimeSeconds(_liveProgram.BeginTime);
        DateTimeOffset? _EndTime;
        public DateTimeOffset EndTime => _EndTime ??= DateTimeOffset.FromUnixTimeSeconds(_liveProgram.EndTime);

        public void ScheduleUpdated(DateTimeOffset startTime, DateTimeOffset endTime)
        {
            _StartTime = startTime;
            _EndTime = endTime;
        }

        private TimeSpan? _DurationFromStart;
        public TimeSpan DurationFromStart
        {
            get { return _DurationFromStart ??= EndTime - StartTime; }
        }

        private TimeSpan? _DurationFromOpen;
        public TimeSpan DurationFromOpen
        {
            get { return _DurationFromOpen ??= EndTime - OpenTime; }
        }

        private TimeSpan? _DurationBtwOpenToStart;
        public TimeSpan DurationBtwOpenToStart
        {
            get { return _DurationBtwOpenToStart ??= StartTime - OpenTime; }
        }


        public TimeSpan PlaybackHeadPosition { get; set; }


        // 現在時刻を放送開始時間に射影するためのオフセット時間
        private TimeSpan _ToOpenTimeOffset;

        Func<TimeSpan, LivePositionResult> _updateMethod;

        public LivePositionResult UpdatePlaybackTime(TimeSpan playbackPosition)
        {
            return _updateMethod(playbackPosition);
        }


        public struct LivePositionResult
        {
            public TimeSpan LiveElapsedTimeFromOpen;
            public TimeSpan LiveElapsedTime;
            public bool IsReachEndedTime;
        }
        

        private LivePositionResult UpdateLiveElapsedTimeAsync_Timeshift(TimeSpan position)
        {
            var liveElpasedTime = position + PlaybackHeadPosition;
            return new LivePositionResult
            {
                LiveElapsedTime = liveElpasedTime,
                LiveElapsedTimeFromOpen = liveElpasedTime - DurationBtwOpenToStart,
                IsReachEndedTime = (EndTime - StartTime) <= liveElpasedTime
            };
        }

        private LivePositionResult UpdateLiveElapsedTimeAsync_LiveStreaming(TimeSpan position)
        {
            // PlaybackHeadPosition が追っかけ再生時に -00:20:00 のように負の時間であると想定
            var jpDateTime = DateTimeOffset.Now.ToOffset(JapanTimeZoneOffset);
            return new LivePositionResult
            {
                LiveElapsedTime = jpDateTime - StartTime + PlaybackHeadPosition,
                LiveElapsedTimeFromOpen = jpDateTime - OpenTime + PlaybackHeadPosition,
                IsReachEndedTime = EndTime <= jpDateTime + PlaybackHeadPosition
            };
        }

    }

}
