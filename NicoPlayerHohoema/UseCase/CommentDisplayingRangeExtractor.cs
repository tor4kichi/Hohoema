﻿using NiconicoLiveToolkit.Live.WatchSession;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Models.Niconico;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.UseCase
{
    public ref struct CommentDisplayingRangeChanged
    {
        public ReadOnlySpan<Comment> InRangeComments;
        public ReadOnlySpan<Comment> AddedComments;
        public ReadOnlySpan<Comment> RemovedComments;
    }

    public sealed class CommentDisplayingRangeExtractor
    {
        public CommentDisplayingRangeExtractor(PlayerSettings playerSettings)
        {
            _playerSettings = playerSettings;
        }


        Comment[] _comments;
        TimeSpan _prevPosition;
        int _endIndex = 0;
        int _startIndex = 0;
        private readonly PlayerSettings _playerSettings;

        public void Clear()
        {
            _comments = null;
            _prevPosition = default;
            _startIndex = 0;
            _endIndex = 0;
        }

        public ReadOnlySpan<Comment> ResetComments(IEnumerable<Comment> comments, TimeSpan initialPosition = default)
        {
            Clear();

            _comments = comments.OrderBy(x => x.VideoPosition).ToArray();

            return Rewind(initialPosition);
        }

        public CommentDisplayingRangeChanged UpdateToNextFrame(TimeSpan currentPosition)
        {
            if (_comments == null) { return new CommentDisplayingRangeChanged(); }

            TimeSpan startPosition = GetDisplayRangeStartPosition(currentPosition);

            if (Math.Abs((_prevPosition - currentPosition).TotalSeconds) >= 1.0) 
            {
                // 1秒以上飛んでいた場合はリセットをかける
                var prevStart = _startIndex;
                var prevEnd = _endIndex;

                var comments = Rewind(currentPosition);

                return new CommentDisplayingRangeChanged()
                {
                    InRangeComments = comments,
                    AddedComments = comments,
                    RemovedComments = new ReadOnlySpan<Comment>(_comments, prevStart, prevEnd - prevStart)
                };
            }

            var rangeComments = EnumerateCommentsInRange(ref _startIndex, ref _endIndex, startPosition, currentPosition);

            _prevPosition = currentPosition;

            return rangeComments;
        }

        public ReadOnlySpan<Comment> Rewind(TimeSpan endPosition)
        {
            if (_comments == null) { return ReadOnlySpan<Comment>.Empty; }

            var startPosition = GetDisplayRangeStartPosition(endPosition);
            _startIndex = 0;
            _endIndex = 0;
            foreach (var comment in _comments)
            {
                if (comment.VideoPosition < startPosition) { ++_startIndex; }
                if (comment.VideoPosition < endPosition) { ++_endIndex; }
            }

            var displayComments = EnumerateCommentsInRange(ref _startIndex, ref _endIndex, startPosition, endPosition);

            _prevPosition = endPosition;

            return displayComments.InRangeComments;
        }


        private CommentDisplayingRangeChanged EnumerateCommentsInRange(ref int start, ref int end, TimeSpan startPosition, TimeSpan endPosition)
        {
            int prevStart = start;
            int newStart = start;
            foreach (var comment in _comments.Skip(prevStart))
            {
                if (comment.VideoPosition >= startPosition) { break; }
                ++newStart;
            }

            int prevEnd = end;
            int newEnd = end;
            foreach (var comment in _comments.Skip(prevEnd))
            {
                if (comment.VideoPosition >= endPosition) { break; }
                ++newEnd;
            }

            start = newStart;
            end = newEnd;

#if DEBUG
            var hasNewStart = prevStart != newStart;
            var hasNewEnd = prevEnd != newEnd;

            if (hasNewStart && hasNewEnd)
            {
                System.Diagnostics.Debug.WriteLine($"removed: {prevStart} - {newStart - 1}, added: {prevEnd} - {newEnd - 1}");
            }
            else if (hasNewStart && !hasNewEnd)
            {
                System.Diagnostics.Debug.WriteLine($"removed: {prevStart} - {newStart - 1}");
            }
            else if (!hasNewStart && hasNewEnd)
            {
                System.Diagnostics.Debug.WriteLine($"added: {prevEnd} - {newEnd - 1}");
            }
#endif 

            return new CommentDisplayingRangeChanged()
            {
                InRangeComments = new ReadOnlySpan<Comment>(_comments, newStart, newEnd - newStart),
                RemovedComments = new ReadOnlySpan<Comment>(_comments, prevStart, newStart - prevStart),
                AddedComments = new ReadOnlySpan<Comment>(_comments, prevEnd, newEnd - prevEnd),
            };
        }

        private TimeSpan GetDisplayRangeStartPosition(TimeSpan currentPosition)
        {
            // ２秒表示時間を伸ばすといい感じに表示できる（邪悪）
            return currentPosition - _playerSettings.CommentDisplayDuration - TimeSpan.FromSeconds(2); 
        }


    }
}
