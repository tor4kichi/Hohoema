using System;
using System.Collections.Generic;
using Windows.UI;

namespace Hohoema.Models.Player.Comment;

public interface IComment
{
    uint CommentId { get; }
    string CommentText { get; }
    int DeletedFlag { get; }
    bool IsAnonymity { get; set; }
    bool IsInvisible { get; set; }
    bool IsLoginUserComment { get; }
    bool IsOwnerComment { get; }
    IReadOnlyList<string> Commands { get; }
    string UserId { get; }
    TimeSpan VideoPosition { get; }
    int NGScore { get; }

    Color? Color { get; set; }
    string CommentText_Transformed { get; set; }
    CommentDisplayMode DisplayMode { get; set; }
    bool IsScrolling { get; }
    CommentSizeMode SizeMode { get; set; }

    void ApplyCommands();
}

public static class CommentExtensions
{
    public static string GetJoinedCommandsText(this IComment comment)
    {
        return string.Join(' ', comment.Commands);
    }
}
