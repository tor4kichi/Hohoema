﻿using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Player;
using Hohoema.Models.Domain.Player.Comment;
using Hohoema.Models.Domain.Player.Video.Cache;
using Hohoema.Models.Domain.Player.Video.Comment;
using Hohoema.Models.Infrastructure;
using Hohoema.Presentation.Services;
using Microsoft.AppCenter.Analytics;
using MvvmHelpers;
using NiconicoToolkit.Video.Watch.NMSG_Comment;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno.Threading;
using Windows.Media.Playback;
using Windows.System;

namespace Hohoema.Models.UseCase.Niconico.Player.Comment
{
    public class CommentPlayer : FixPrism.BindableBase, IDisposable
    {
        CompositeDisposable _disposables = new CompositeDisposable();

        private readonly IScheduler _scheduler;
        private readonly CommentDisplayingRangeExtractor _commentDisplayingRangeExtractor;
        private readonly CommentFilteringFacade _commentFiltering;
        private readonly NotificationService _notificationService;
        private readonly PlayerSettings _playerSettings;
        private INiconicoCommentSessionProvider _niconicoCommentSessionProvider;
        private readonly MediaPlayer _mediaPlayer;

        public ObservableRangeCollection<VideoComment> Comments { get; private set; }
        public ObservableRangeCollection<VideoComment> DisplayingComments { get; } = new ObservableRangeCollection<VideoComment>();
        public ReactiveProperty<string> WritingComment { get; private set; }
        public ReactiveProperty<string> CommandText { get; private set; }
        public ReactiveProperty<bool> NowSubmittingComment { get; private set; }

        public AsyncReactiveCommand CommentSubmitCommand { get; }
        ICommentSession _commentSession;

        FastAsyncLock _commentUpdateLock = new FastAsyncLock();

        private bool _nowSeekDisabledFromNicoScript;
        public bool NowSeekDisabledFromNicoScript
        {
            get => _nowSeekDisabledFromNicoScript;
            set => SetProperty(ref _nowSeekDisabledFromNicoScript, value);
        }

        private bool _nowCommentSubmitDisabledFromNicoScript;
        public bool NowCommentSubmitDisabledFromNicoScript
        {
            get => _nowCommentSubmitDisabledFromNicoScript;
            set => SetProperty(ref _nowCommentSubmitDisabledFromNicoScript, value);
        }


        event EventHandler<string> NicoScriptJumpVideoRequested;
        event EventHandler<TimeSpan> NicoScriptJumpTimeRequested;
        event EventHandler CommentSubmitFailed;

        public CommentPlayer(
            MediaPlayer mediaPlayer, 
            IScheduler scheduler,
            CommentDisplayingRangeExtractor commentDisplayingRangeExtractor,
            CommentFilteringFacade commentFiltering,
            NotificationService notificationService,
            PlayerSettings playerSettings
            )
        {
            _mediaPlayer = mediaPlayer;
            _scheduler = scheduler;
            _commentDisplayingRangeExtractor = commentDisplayingRangeExtractor;
            _commentFiltering = commentFiltering;
            _notificationService = notificationService;
            _playerSettings = playerSettings;
            Comments = new ObservableRangeCollection<VideoComment>();

            CommentSubmitCommand = new AsyncReactiveCommand()
                .AddTo(_disposables);

            CommentSubmitCommand.Subscribe(async _ => await SubmitComment())
                .AddTo(_disposables);

            WritingComment = new ReactiveProperty<string>(_scheduler, string.Empty)
                .AddTo(_disposables);

            NowSubmittingComment = new ReactiveProperty<bool>(_scheduler)
                .AddTo(_disposables);


            CommandText = new ReactiveProperty<string>(_scheduler, string.Empty, ReactivePropertyMode.DistinctUntilChanged)
                .AddTo(_disposables);

            new[]
            {
                _commentFiltering.ObserveProperty(x => x.IsEnableFilteringCommentOwnerId).ToUnit(),
                Observable.FromEventPattern<CommentFilteringFacade.CommentOwnerIdFilteredEventArgs>(
                    h => _commentFiltering.FilteringCommentOwnerIdAdded += h,
                    h => _commentFiltering.FilteringCommentOwnerIdAdded -= h
                ).ToUnit(),
                Observable.FromEventPattern<CommentFilteringFacade.CommentOwnerIdFilteredEventArgs>(
                    h => _commentFiltering.FilteringCommentOwnerIdRemoved += h,
                    h => _commentFiltering.FilteringCommentOwnerIdRemoved -= h
                ).ToUnit(),

                _commentFiltering.ObserveProperty(x => x.IsEnableFilteringCommentText).ToUnit(),
                Observable.FromEventPattern<CommentFilteringFacade.FilteringCommentTextKeywordEventArgs>(
                    h => _commentFiltering.FilterKeywordAdded += h,
                    h => _commentFiltering.FilterKeywordAdded -= h
                ).ToUnit(),
                Observable.FromEventPattern<CommentFilteringFacade.FilteringCommentTextKeywordEventArgs>(
                    h => _commentFiltering.FilterKeywordRemoved += h,
                    h => _commentFiltering.FilterKeywordRemoved -= h
                ).ToUnit(),
                Observable.FromEventPattern<CommentFilteringFacade.FilteringCommentTextKeywordEventArgs>(
                    h => _commentFiltering.FilterKeywordUpdated += h,
                    h => _commentFiltering.FilterKeywordUpdated -= h
                ).ToUnit(),

                Observable.FromEventPattern<CommentFilteringFacade.CommentTextTranformConditionChangedArgs>(
                    h => _commentFiltering.TransformConditionAdded += h,
                    h => _commentFiltering.TransformConditionAdded -= h
                ).ToUnit(),
                Observable.FromEventPattern<CommentFilteringFacade.CommentTextTranformConditionChangedArgs>(
                    h => _commentFiltering.TransformConditionUpdated += h,
                    h => _commentFiltering.TransformConditionUpdated -= h
                ).ToUnit(),
                Observable.FromEventPattern<CommentFilteringFacade.CommentTextTranformConditionChangedArgs>(
                    h => _commentFiltering.TransformConditionRemoved += h,
                    h => _commentFiltering.TransformConditionRemoved -= h
                ).ToUnit(),
                _commentFiltering.ObserveProperty(x => x.ShareNGScore).ToUnit(),
            }
            .Merge()
            .Subscribe(_ => RefreshFiltering())
            .AddTo(_disposables);

            var dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _CommentUpdateTimer = dispatcherQueue.CreateTimer();
            _CommentUpdateTimer.Tick += _CommentUpdateTimer_Tick;
            _CommentUpdateTimer.Interval = TimeSpan.FromMilliseconds(50);
            _CommentUpdateTimer.IsRepeating = true;
        }

        void RefreshFiltering()
        {
            HiddenCommentIds.Clear();
            
            var displayingComments = _commentDisplayingRangeExtractor.Rewind(_mediaPlayer.PlaybackSession.Position);

            DisplayingComments.Clear();
            foreach (var comment in EnumerateFilteredDisplayComment(displayingComments.ToArray()))
            {
                DisplayingComments.Add(comment);
            }
        }

        private void RewindAsync(TimeSpan position)
        {
            DisplayingComments.Clear();
            var displayingComments = _commentDisplayingRangeExtractor.Rewind(position);
            foreach (var comment in EnumerateFilteredDisplayComment(displayingComments.ToArray()))
            {
                DisplayingComments.Add(comment);
            }
        }



        public void Dispose()
        {
            _CommentUpdateTimer.Stop();
            _CommentUpdateTimer = null;

            ClearCurrentSession();

            _disposables.Dispose();
        }


        public void ClearCurrentSession()
        {
            _CommentUpdateTimer.Stop();

            _commentSession?.Dispose();
            _commentSession = null;
            
            _niconicoCommentSessionProvider = null;

            ClearNicoScriptState();

            Comments.Clear();
            DisplayingComments.Clear();

            WritingComment.Value = string.Empty;

            CurrentCommentIndex = 0;
            CurrentComment = null;
        }


        public async Task UpdatePlayingCommentAsync(INiconicoCommentSessionProvider niconicoCommentSessionProvider, CancellationToken ct = default)
        {
            using (await _commentUpdateLock.LockAsync(ct))
            {
                _niconicoCommentSessionProvider = niconicoCommentSessionProvider;

                if (_niconicoCommentSessionProvider == null) { return; }

                try
                {
                    var commentSession = await _niconicoCommentSessionProvider.CreateCommentSessionAsync();

                    // コミュニティやチャンネルの動画では匿名コメントは利用できない
                    //CommandEditerVM.ChangeEnableAnonymity(CommentClient.IsAllowAnnonimityComment);

                    // コメントの更新
                    await UpdateComments_Internal(commentSession);

                    _commentSession = commentSession;

                    _CommentUpdateTimer.Start();
                }
                catch
                {

                }
            }
        }

        private void _CommentUpdateTimer_Tick(DispatcherQueueTimer sender, object args)
        {
            var session = _mediaPlayer.PlaybackSession;

            var currentPosition = session.Position;
            if (_PrevPlaybackPosition == currentPosition) { return; }

            if ((currentPosition - _PrevPlaybackPosition).Duration() > TimeSpan.FromSeconds(1))
            {
                CurrentCommentIndex = 0;
                CurrentComment = null;
            }

            TickNextDisplayingComments(currentPosition);
            UpdateNicoScriptComment(currentPosition);
            RefreshCurrentPlaybackPositionComment(currentPosition);

            _PrevPlaybackPosition = currentPosition;
        }

        public async Task RefreshComments()
        {
            if (_commentSession == null) { return; }

            using (await _commentUpdateLock.LockAsync(default))
            {
                await UpdateComments_Internal(_commentSession);
            }
        }

        

        private async Task SubmitComment()
        {
            using var _lock = await _commentUpdateLock.LockAsync(default);

            if (_commentSession == null) { return; }

            NowSubmittingComment.Value = true;

            Debug.WriteLine($"try comment submit:{WritingComment.Value}");

            var postComment = WritingComment.Value;
            var posision = _mediaPlayer.PlaybackSession.Position;
            var command = CommandText.Value;

            try
            {
                CommentPostResult res = await _commentSession.PostComment(postComment, posision, command);

                if (res.Status == ChatResultCode.Success)
                {
                    Debug.WriteLine("コメントの投稿に成功: " + res.CommentNo);

                    VideoComment videoComment = new()
                    {
                        CommentId = (uint)res.CommentNo,
                        VideoPosition = posision,
                        UserId = _commentSession.UserId,
                        CommentText = postComment,
                    };

                    foreach (var action in MailToCommandHelper.MakeCommandActions(command.Split(' ')))
                    {
                        action(videoComment);
                    }

                    Comments.Add(videoComment);

                    ResetDisplayingComments(Comments);

                    WritingComment.Value = "";
                }
                else
                {
                    CommentSubmitFailed?.Invoke(this, EventArgs.Empty);

                    _notificationService.ShowLiteInAppNotification_Fail($"{_commentSession.ContentId} へのコメント投稿に失敗\n ステータスコード：{res.StatusCode}");

                    ErrorTrackingManager.TrackError(new HohoemaExpception("SubmitComment Failed"), new Dictionary<string, string>()
                    {
                        { "ContentId",  _commentSession.ContentId },
                        { "Command", command },
                        { "CommentLength", postComment?.Length.ToString() },
                        { "StatusCode", res.StatusCode.ToString() },
                    });
                }
            }
            catch (NotSupportedException ex)
            {
                Debug.WriteLine(ex.ToString());
            }
            finally
            {
                NowSubmittingComment.Value = false;
            }
            
        }




        private async Task UpdateComments_Internal(ICommentSession commentSession)
        {
            // ニコスクリプトの状態を初期化
            ClearNicoScriptState();

            Comments.Clear();
            DisplayingComments.Clear();
            HiddenCommentIds.Clear();

            var comments =  await commentSession.GetInitialComments();

            IEnumerable<VideoComment> commentsAction(IEnumerable<VideoComment> comments)
            {
                foreach (var comment in comments)
                {
                    if (comment.UserId == null)
                    {
                        if (comment.DeletedFlag > 0) { continue; }
                        if (TryAddNicoScript(comment))
                        {
                            // 投コメのニコスクリプトをスキップして
                            continue;
                        }
                    }

                    yield return comment;
                }
            }

            Comments.AddRange(commentsAction(comments.Cast<VideoComment>().OrderBy(x => x.VideoPosition)));

            ResetDisplayingComments(Comments);

            _NicoScriptList.Sort((x, y) => (int)(x.BeginTime.Ticks - y.BeginTime.Ticks));
            System.Diagnostics.Debug.WriteLine($"コメント数:{Comments.Count}");
        }


        void ResetDisplayingComments(IReadOnlyCollection<VideoComment> comments)
        {
            DisplayingComments.Clear();
            Debug.WriteLine($"CommentReset");

            var displayingComments = _commentDisplayingRangeExtractor.ResetComments(comments, _mediaPlayer.PlaybackSession.Position);
            DisplayingComments.AddRange(EnumerateFilteredDisplayComment(displayingComments.ToArray()));
        }



        void TickNextDisplayingComments(TimeSpan position)
        {
            var comments = _commentDisplayingRangeExtractor.UpdateToNextFrame(position);

            if (!comments.RemovedComments.IsEmpty)
            {
                DisplayingComments.RemoveRange(comments.RemovedComments.ToArray(), System.Collections.Specialized.NotifyCollectionChangedAction.Remove);
            }

            if (!comments.AddedComments.IsEmpty)
            {
                DisplayingComments.AddRange(EnumerateFilteredDisplayComment(comments.AddedComments.ToArray()));
            }
        }

        bool IsHiddenComment(VideoComment comment)
        {
            if (_commentFiltering.IsHiddenComment(comment))
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(comment.Mail))
            {
                var commands = comment.Mail.Split(' ');
                if (commands.Any(x => _commentFiltering.IsIgnoreCommand(x)))
                {
                    return true;
                }

                // 効率的実行のためやむなくここでコマンドを処理
                foreach (var action in MailToCommandHelper.MakeCommandActions(commands))
                {
                    action(comment);
                }
            }

            return false;
        }

        bool IsHiddenCommentWithCache(VideoComment comment)
        {
            if (HiddenCommentIds.TryGetValue(comment.CommentId, out bool isHiddenComment))
            {
                return isHiddenComment;
            }
            else
            {
                isHiddenComment = IsHiddenComment(comment);
                HiddenCommentIds.Add(comment.CommentId, isHiddenComment);
                if (!isHiddenComment)
                {
                    comment.CommentText_Transformed = _commentFiltering.TransformCommentText(comment.CommentText);
                }

                return isHiddenComment;
            }
        }

        IEnumerable<VideoComment> EnumerateFilteredDisplayComment(IEnumerable<VideoComment> comments)
        {
            foreach (var comment in comments)
            {
                if (!IsHiddenCommentWithCache(comment))
                {
                    yield return comment;
                }
            }
        }


        Dictionary<uint, bool> HiddenCommentIds = new Dictionary<uint, bool>();

        TimeSpan _PrevPlaybackPosition;

        void UpdateNicoScriptComment(TimeSpan position)
        {
            foreach (var script in _NicoScriptList)
            {
                if (_PrevPlaybackPosition <= script.BeginTime && position > script.BeginTime)
                {
                    if (script.EndTime < position)
                    {
                        Debug.WriteLine("nicoscript Enabling Skiped :" + script.Type);
                        continue;
                    }

                    Debug.WriteLine("nicoscript Enabling :" + script.Type);
                    script.ScriptEnabling?.Invoke();
                }
                else if (script.EndTime.HasValue)
                {
                    if (_PrevPlaybackPosition <= script.BeginTime)
                    {
                        Debug.WriteLine("nicoscript Disabling Skiped :" + script.Type);
                        continue;
                    }

                    if (_PrevPlaybackPosition < script.EndTime && position > script.EndTime)
                    {
                        Debug.WriteLine("nicoscript Disabling :" + script.Type);
                        script.ScriptDisabling?.Invoke();
                    }
                }
            }

        }

        void RefreshCurrentPlaybackPositionComment(TimeSpan position)
        {
            // TODO: Commentsにアクセスする際の非同期ロック
            var currentIndex = CurrentCommentIndex;
            foreach (var comment in Comments.Skip(CurrentCommentIndex).Cast<VideoComment>())
            {
                if ((comment as VideoComment).VideoPosition > position)
                {
                    CurrentComment = comment;
                    break;
                }

                ++currentIndex;
            }

            CurrentCommentIndex = currentIndex;
        }

        private VideoComment _CurrentComment;
        public VideoComment CurrentComment
        {
            get { return _CurrentComment; }
            private set { SetProperty(ref _CurrentComment, value); }
        }

        private int _currentCommentIndex;
        public int CurrentCommentIndex
        {
            get { return _currentCommentIndex; }
            private set { SetProperty(ref _currentCommentIndex, value); }
        }


        #region 



        List<NicoScript> _NicoScriptList = new List<NicoScript>();
        List<ReplaceNicoScript> _ReplaceNicoScirptList = new List<ReplaceNicoScript>();
        List<DefaultCommandNicoScript> _DefaultCommandNicoScriptList = new List<DefaultCommandNicoScript>();
        private DispatcherQueueTimer _CommentUpdateTimer;

        private static bool IsNicoScriptComment(string userId, string content)
        {
            if (userId != null) { return false; }

            if (string.IsNullOrEmpty(content)) { return false; }

            return (content.StartsWith("＠") || content.StartsWith("@") || content.StartsWith("/"));
        }



        private bool TryAddNicoScript(VideoComment chat)
        {
            const bool IS_ENABLE_Default = true; // Default comment Command
            const bool IS_ENABLE_Replace = false; // Replace comment text
            const bool IS_ENABLE_Jump = true; // seek video position or redirect to another content
            const bool IS_ENABLE_DisallowSeek = true; // disable seek
            const bool IS_ENABLE_DisallowComment = true; // disable comment

            if (!IsNicoScriptComment(chat.UserId, chat.CommentText)) { return false; }

            var nicoScriptContents = chat.CommentText.Remove(0, 1).Split(' ', '　');

            if (nicoScriptContents.Length == 0) { return false; }

            var nicoScriptType = nicoScriptContents[0];
            var beginTime = chat.VideoPosition;
            switch (nicoScriptType)
            {
                case "デフォルト":
                    if (IS_ENABLE_Default)
                    {
                        TimeSpan? endTime = null;
                        var commands = chat.Mail.Split(' ');
                        var timeCommand = commands.FirstOrDefault(x => x.StartsWith("@"));
                        if (timeCommand != null)
                        {
                            endTime = beginTime + TimeSpan.FromSeconds(int.Parse(timeCommand.Remove(0, 1)));
                        }

                        _DefaultCommandNicoScriptList.Add(new DefaultCommandNicoScript(nicoScriptType)
                        {
                            BeginTime = beginTime,
                            EndTime = endTime,
                            Command = commands.Where(x => !x.StartsWith("@")).ToArray()
                        });

                        // CommandEditer に対して設定する？
                    }


                    break;
                case "置換":
                    if (IS_ENABLE_Replace)
                    {
                        var commands = chat.Mail.Split(' ');
                        List<string> commandItems = new List<string>();
                        TimeSpan duration = TimeSpan.FromSeconds(30);
                        foreach (var command in commands)
                        {
                            if (command.StartsWith("@"))
                            {
                                duration = TimeSpan.FromSeconds(int.Parse(command.Remove(0, 1)));
                            }
                            else
                            {
                                commandItems.Add(command);
                            }
                        }
                        /*
                         * ※1 オプション自体にスペースを含めたい場合は、コマンドをダブルクォート(")、シングルクォート(')、または全角かぎかっこ（「」）で囲んでください。
                            その際、ダブルクォート(")とシングルクォート(')内ではバックスラッシュ（\）がエスケープ文字として扱われますが、全角かぎかっこ（「」）内では文字列として扱われます。
  
                         */

                        _ReplaceNicoScirptList.Add(new ReplaceNicoScript(nicoScriptType)
                        {
                            Commands = string.Join(" ", commandItems),
                            BeginTime = beginTime,
                            EndTime = beginTime + duration,

                        });

                        Debug.WriteLine($"置換を設定");
                    }
                    break;
                case "ジャンプ":
                    if (IS_ENABLE_Jump)
                    {
                        var condition = nicoScriptContents[1];
                        if (condition.StartsWith("#"))
                        {
                            TimeSpan? endTime = null;
                            if (chat.Mail?.StartsWith("@") ?? false)
                            {
                                endTime = beginTime + TimeSpan.FromSeconds(int.Parse(chat.Mail.Remove(0, 1)));
                            }
                            _NicoScriptList.Add(new NicoScript(nicoScriptType)
                            {
                                BeginTime = beginTime,
                                EndTime = endTime,
                                ScriptEnabling = () =>
                                {
                                    // #00:00
                                    // #00:00.00
                                    // #00
                                    // #00.00
                                    TimeSpan time = TimeSpan.Zero;
                                    var timeTexts = condition.Remove(0, 1).Split(':');
                                    if (timeTexts.Length == 2)
                                    {
                                        time += TimeSpan.FromMinutes(int.Parse(timeTexts[0]));
                                    }

                                    var secAndMillSec = timeTexts.Last().Split('.');

                                    var sec = secAndMillSec.First();
                                    time += TimeSpan.FromSeconds(int.Parse(sec));
                                    if (secAndMillSec.Length == 2)
                                    {
                                        time += TimeSpan.FromMilliseconds(int.Parse(secAndMillSec.Last()));
                                    }

                                    NicoScriptJumpTimeRequested?.Invoke(this, time);
                                }
                            });

                            Debug.WriteLine($"{beginTime.ToString()} に {condition} へのジャンプを設定");
                        }
                        else if (NiconicoToolkit.ContentIdHelper.IsVideoId(condition))
                        {
                            var message = nicoScriptContents.ElementAtOrDefault(2);

                            TimeSpan endTime = _mediaPlayer.PlaybackSession.NaturalDuration;
                            var commands = chat.Mail?.Split(' ') ?? new string[0];
                            var timeCommand = commands.FirstOrDefault(x => x.StartsWith("@"));
                            if (timeCommand != null)
                            {
                                endTime = beginTime + TimeSpan.FromSeconds(int.Parse(timeCommand.Remove(0, 1)));
                            }
                            _NicoScriptList.Add(new NicoScript(nicoScriptType)
                            {
                                BeginTime = beginTime,
                                EndTime = endTime,
                                ScriptEnabling = () =>
                                {
                                    Debug.WriteLine($"{beginTime.ToString()} に {condition} へのジャンプを設定");
                                    NicoScriptJumpVideoRequested?.Invoke(this, condition);
                                }
                            });
                        }
                    }
                    break;
                case "シーク禁止":
                    if (IS_ENABLE_DisallowSeek)
                    {
                        TimeSpan? endTime = null;
                        if (chat.Mail?.StartsWith("@") ?? false)
                        {
                            endTime = beginTime + TimeSpan.FromSeconds(int.Parse(chat.Mail.Remove(0, 1)));
                        }
                        _NicoScriptList.Add(new NicoScript(nicoScriptType)
                        {
                            BeginTime = beginTime,
                            EndTime = endTime,
                            ScriptEnabling = () => NowSeekDisabledFromNicoScript = true,
                            ScriptDisabling = () => NowSeekDisabledFromNicoScript = false
                        });

                        if (endTime.HasValue)
                        {
                            Debug.WriteLine($"{beginTime.ToString()} ～ {endTime.ToString()} までシーク禁止を設定");
                        }
                        else
                        {
                            Debug.WriteLine($"{beginTime.ToString()} から動画終了までシーク禁止を設定");
                        }
                    }
                    break;
                case "コメント禁止":
                    if (IS_ENABLE_DisallowComment)
                    {
                        TimeSpan? endTime = null;
                        if (chat.Mail?.StartsWith("@") ?? false)
                        {
                            endTime = beginTime + TimeSpan.FromSeconds(int.Parse(chat.Mail.Remove(0, 1)));
                        }
                        _NicoScriptList.Add(new NicoScript(nicoScriptType)
                        {
                            BeginTime = beginTime,
                            EndTime = endTime,
                            ScriptEnabling = () => NowCommentSubmitDisabledFromNicoScript = true,
                            ScriptDisabling = () => NowCommentSubmitDisabledFromNicoScript = false
                        });
#if DEBUG
                        if (endTime.HasValue)
                        {
                            Debug.WriteLine($"{beginTime.ToString()} ～ {endTime.ToString()} までコメント禁止を設定");
                        }
                        else
                        {
                            Debug.WriteLine($"{beginTime.ToString()} から動画終了までコメント禁止を設定");
                        }
#endif
                    }
                    break;
                default:
                    Debug.WriteLine($"Not support nico script type : {nicoScriptType}");
                    break;
            }

            return true;
        }

        // コメントの再ロード時などにニコスクリプトを再評価する
        private void ClearNicoScriptState()
        {
            // デフォルトのコメントコマンドをクリア
            _DefaultCommandNicoScriptList.Clear();

            // 置換設定をクリア
            _ReplaceNicoScirptList.Clear();

            // ジャンプスクリプト
            // シーク禁止とコメント禁止
            _NicoScriptList.Clear();

            NowSeekDisabledFromNicoScript = false;
            NowCommentSubmitDisabledFromNicoScript = false;
        }


        #endregion
    }
}
