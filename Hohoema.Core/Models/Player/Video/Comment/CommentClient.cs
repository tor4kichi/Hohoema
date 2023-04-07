using NiconicoToolkit.Live.WatchSession;
using Hohoema.Helpers;
using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Video;
using Hohoema.Infra;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NiconicoSession = Hohoema.Models.Niconico.NiconicoSession;
using NiconicoToolkit.Video.Watch;
using Hohoema.Models.Player.Comment;
using NiconicoToolkit.Video.Watch.NV_Comment;
using CommunityToolkit.Diagnostics;
using NiconicoToolkit.Video;
using System.Threading;
using NiconicoToolkit.Live.WatchPageProp;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Runtime.Serialization;
using Windows.UI;


#nullable enable

namespace Hohoema.Models.Player.Video.Comment;


public class CommentClient
{
    // コメントの取得、とアプリケーションドメインなコメントモデルへの変換
    // コメントの投稿可否判断と投稿処理

    public CommentClient(NiconicoSession niconicoSession, string rawVideoid)
    {
        _niconicoSession = niconicoSession;
        RawVideoId = rawVideoid;        
    }

    public string RawVideoId { get; }
    public string VideoOwnerId => _watchApiData!.Owner.Id.ToString();

    internal DmcWatchApiData? _watchApiData { get; set; }


    private readonly NiconicoSession _niconicoSession;
    private NiconicoToolkit.NiconicoContext _toolkitContext => _niconicoSession.ToolkitContext;
    
    private NvCommentSubClient _nvCommentApi => _niconicoSession.ToolkitContext.Video.NvComment;

    public async Task<CommentPostResult> SubmitComment(string comment, TimeSpan position, string commands)
    {
        CancellationToken ct = default;
        Guard.IsNotNull(_watchApiData);

        VideoId videoId = _watchApiData.Video.Id;
        var mainThread = _watchApiData.Comment.Threads.First(x => x.ForkLabel == ThreadTargetForkConstants.Main);
        string threadId = mainThread.Id.ToString();

        bool isPostCompleted = false;
        ThreadPostResponse.ThreadPostResponseData? chatRes = null;
        foreach (var i in Enumerable.Range(0, 2))
        {
            string postKey = await GetPostKeyWithCacheAsync(mainThread.ForkLabel, ct);
            var res = await _nvCommentApi.PostCommentAsync(threadId, videoId, commands.Split(' '), comment, (int)position.TotalMilliseconds, postKey, ct);

            if (res.IsSuccess)
            {
                isPostCompleted = true;
                chatRes = res.Data;
                break;
            }
            else
            {
                ClearPostKeyCache();
            }
        }

        return new CommentPostResult()
        {
            CommentNo = chatRes?.Number ?? -1, 
            IsSuccessed = isPostCompleted,
            ThreadId = threadId,
            VideoPosition = position,
            ThreadForkLabel = mainThread.ForkLabel,
        };
    }

    public bool IsAllowAnnonimityComment
    {
        get
        {
            if (_watchApiData == null) { return false; }

            if (_watchApiData.Channel != null) { return false; }

            if (_watchApiData.Community != null) { return false; }

            return true;
        }
    }

    public bool CanSubmitComment
    {
        get
        {
            if (!Helpers.InternetConnection.IsInternet()) { return false; }

            if (_watchApiData?.Comment?.NvComment == null) { return false; }            

            return true;
        }
    }


    public async Task<IEnumerable<IVideoComment>> GetCommentsAsync()
    {
        var commentRes = await _nvCommentApi.GetCommentsAsync(_watchApiData!.Comment.NvComment, ct: default);
        return commentRes.Data.Threads.SelectMany(x => x.Comments).OrderBy(x => x.VposMs).Select(ToVideoComent);
    }

    //private VideoComment ChatToComment(NMSG_Chat rawComment)
    //{
    //    return new VideoComment()
    //    {
    //        CommentText = rawComment.Content,
    //        CommentId = rawComment.No,
    //        VideoPosition = rawComment.Vpos.ToTimeSpan(),
    //        UserId = rawComment.UserId,
    //        Mail = rawComment.Mail,
    //        NGScore = rawComment.Score ?? 0,
    //        IsAnonymity = rawComment.Anonymity != 0,
    //        IsLoginUserComment = _niconicoSession.IsLoggedIn && rawComment.Anonymity == 0 && rawComment.UserId == _niconicoSession.UserId,
    //        //IsOwnerComment = rawComment.UserId != null && rawComment.UserId == VideoOwnerId,
    //        DeletedFlag = rawComment.Deleted ?? 0
    //    };
    //}



    private static NvVideoComment ToVideoComent(ThreadResponse.Comment nvComment)
    {
        return new NvVideoComment(nvComment);
    }

    private Dictionary<string, string> _threadForkToPostKeyCachedMap = new();
    private VideoId? _lastThreadForkToPostKeyVideoId;

    private void ClearPostKeyCache()
    {
        _lastThreadForkToPostKeyVideoId = null;
        _threadForkToPostKeyCachedMap.Clear();
    }
    private async ValueTask<string> GetPostKeyWithCacheAsync(string threadForkLabel, CancellationToken ct = default)
    {
        Guard.IsNotNull(_watchApiData);
        if (_lastThreadForkToPostKeyVideoId is not null and VideoId videoId && videoId == _watchApiData.Video.Id
            && _threadForkToPostKeyCachedMap.TryGetValue(threadForkLabel, out string postKey)
            )
        {
            return postKey;
        }
        else
        {
            _lastThreadForkToPostKeyVideoId = _watchApiData.Video.Id;
            postKey = await GetPostKeyLatestAsync(threadForkLabel, ct);
            _threadForkToPostKeyCachedMap.Add(threadForkLabel, postKey);
            return postKey;
        }
    }

    private async Task<string> GetPostKeyLatestAsync(string threadForkLabel, CancellationToken ct = default)
    {
        if (threadForkLabel == ThreadTargetForkConstants.Main)
        {
            Guard.IsNotNull(_watchApiData);
            var thread = _watchApiData.Comment.Threads.First(x => x.ForkLabel == ThreadTargetForkConstants.Main);
            var res = await _nvCommentApi.GetPostKeyAsync(thread.Id.ToString(), ct);
            Guard.IsTrue(res.IsSuccess);

            return res.Data!.PostKey;
        }
        else if (threadForkLabel == ThreadTargetForkConstants.Easy)
        {
            Guard.IsNotNull(_watchApiData);
            var thread = _watchApiData.Comment.Threads.First(x => x.ForkLabel == ThreadTargetForkConstants.Easy);
            var res = await _nvCommentApi.GetEasyPostKeyAsync(thread.Id.ToString(), ct);
            Guard.IsTrue(res.IsSuccess);

            return res.Data!.EasyPostKey;
        }
        else
        {
            throw new NotSupportedException(threadForkLabel);
        }
    }




}


public class NvVideoComment : ObservableObject, IVideoComment
{
    // コメントのデータ構造だけで他のことを知っているべきじゃない
    // このデータを解釈して実際に表示するためのオブジェクトにする部分は処理は
    // View側が持つべき

    public NvVideoComment(ThreadResponse.Comment comment)
    {
        _comment = comment;
        IsAnonymity = _comment.Commands.Contains("184");
    }

    private readonly ThreadResponse.Comment _comment;

    bool _isAppliedCommands;
    public void ApplyCommands()
    {
        if (_isAppliedCommands) { return; }

        foreach (var action in MailToCommandHelper.MakeCommandActions(Commands))
        {
            action(this);
        }

        _isAppliedCommands = true;
    }

    public uint CommentId => (uint)_comment.No;

    public string CommentText => _comment.Body;

    string? _commands;
    public IReadOnlyList<string> Commands => _comment.Commands;
    public string UserId => _comment.UserId;


    public bool IsAnonymity { get; set; }

    private TimeSpan? _videoPosition;
    public TimeSpan VideoPosition => _videoPosition ??= TimeSpan.FromMilliseconds(_comment.VposMs);

    public int NGScore => _comment.Score;

    public bool IsLoginUserComment => _comment.IsMyPost;

    public bool IsOwnerComment => _comment.Source == ThreadTargetForkConstants.Owner;

    public int DeletedFlag => 0; 


    private string? _commentText_Transformed;
    public string CommentText_Transformed
    {
        get => _commentText_Transformed ?? CommentText;
        set => _commentText_Transformed = value;
    }


    public CommentDisplayMode DisplayMode { get; set; }
    public bool IsScrolling => DisplayMode == CommentDisplayMode.Scrolling;
    public CommentSizeMode SizeMode { get; set; }
    public bool IsInvisible { get; set; }
    public Color? Color { get; set; }
}