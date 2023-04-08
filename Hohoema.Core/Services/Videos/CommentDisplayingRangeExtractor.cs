using Hohoema.Models.Player;
using Hohoema.Models.Player.Comment;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hohoema.Services.Player.Videos;

public ref struct CommentDisplayingRangeChanged<TComment>
{
    public ReadOnlySpan<TComment> InRangeComments;
    public ReadOnlySpan<TComment> AddedComments;
    public ReadOnlySpan<TComment> RemovedComments;
}

public sealed class CommentDisplayingRangeExtractor<TComment> where TComment : IComment
{
    public CommentDisplayingRangeExtractor(PlayerSettings playerSettings)
    {
        _playerSettings = playerSettings;
    }

    private TComment[] _comments;
    private TimeSpan _prevPosition;
    private int _endIndex = 0;
    private int _startIndex = 0;
    private readonly PlayerSettings _playerSettings;

    public void Clear()
    {
        _comments = null;
        _prevPosition = default;
        _startIndex = 0;
        _endIndex = 0;
    }

    public ReadOnlySpan<TComment> ResetComments(IEnumerable<TComment> comments, TimeSpan initialPosition = default)
    {
        Clear();

        _comments = comments.OrderBy(x => x.VideoPosition).ToArray();

        return Rewind(initialPosition);
    }

    [Obsolete]
    public CommentDisplayingRangeChanged<TComment> UpdateToNextFrame(TimeSpan currentPosition)
    {
        if (_comments == null) { return new CommentDisplayingRangeChanged<TComment>(); }

        TimeSpan startPosition = GetDisplayRangeStartPosition(currentPosition);

        if (Math.Abs((_prevPosition - currentPosition).TotalSeconds) >= 1.0)
        {
            // 1秒以上飛んでいた場合はリセットをかける
            int prevStart = _startIndex;
            int prevEnd = _endIndex;

            ReadOnlySpan<TComment> comments = Rewind(currentPosition);

            return new CommentDisplayingRangeChanged<TComment>()
            {
                InRangeComments = comments,
                AddedComments = comments,
                RemovedComments = new ReadOnlySpan<TComment>(_comments, prevStart, prevEnd - prevStart)
            };
        }

        CommentDisplayingRangeChanged<TComment> rangeComments = EnumerateCommentsInRange(ref _startIndex, ref _endIndex, startPosition, currentPosition);

        _prevPosition = currentPosition;

        return rangeComments;
    }

    [Obsolete]
    public ReadOnlySpan<TComment> Rewind(TimeSpan endPosition)
    {
        if (_comments == null) { return ReadOnlySpan<TComment>.Empty; }

        TimeSpan startPosition = GetDisplayRangeStartPosition(endPosition);
        _startIndex = 0;
        _endIndex = 0;
        foreach (TComment comment in _comments)
        {
            if (comment.VideoPosition < startPosition) { ++_startIndex; }
            if (comment.VideoPosition < endPosition) { ++_endIndex; }
        }

        CommentDisplayingRangeChanged<TComment> displayComments = EnumerateCommentsInRange(ref _startIndex, ref _endIndex, startPosition, endPosition);

        _prevPosition = endPosition;

        return displayComments.InRangeComments;
    }


    private CommentDisplayingRangeChanged<TComment> EnumerateCommentsInRange(ref int start, ref int end, TimeSpan startPosition, TimeSpan endPosition)
    {
        int prevStart = start;
        int newStart = start;
        foreach (TComment comment in _comments.Skip(prevStart))
        {
            if (comment.VideoPosition >= startPosition) { break; }
            ++newStart;
        }

        int prevEnd = end;
        int newEnd = end;
        foreach (TComment comment in _comments.Skip(prevEnd))
        {
            if (comment.VideoPosition >= endPosition) { break; }
            ++newEnd;
        }

        start = newStart;
        end = newEnd;

#if false
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

        return new()
        {
            InRangeComments = new(_comments, newStart, newEnd - newStart),
            RemovedComments = new(_comments, prevStart, newStart - prevStart),
            AddedComments = new(_comments, prevEnd, newEnd - prevEnd),
        };
    }

    [Obsolete]
    private TimeSpan GetDisplayRangeStartPosition(TimeSpan currentPosition)
    {
        // ２秒表示時間を伸ばすといい感じに表示できる（邪悪）
        return currentPosition - _playerSettings.CommentDisplayDuration - TimeSpan.FromSeconds(2);
    }


}
