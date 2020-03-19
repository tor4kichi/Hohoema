using NicoPlayerHohoema.Models.Niconico;
using NicoPlayerHohoema.ViewModels;
using Prism.Mvvm;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Prism.Ioc;
using NicoPlayerHohoema.Models.Niconico.Video;
using System.Diagnostics;
using Windows.Media.Playback;
using System.Reactive.Disposables;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;
using Mntone.Nico2.Videos.Comment;
using Mntone.Nico2;
using NicoPlayerHohoema.Repository.NicoVideo;
using NicoPlayerHohoema.UseCase.NicoVideoPlayer;
using Microsoft.Toolkit.Uwp.UI;
using Uno.Extensions;
using MvvmHelpers;

namespace NicoPlayerHohoema.UseCase
{
    public class CommentPlayer : FixPrism.BindableBase, IDisposable
    {
        CompositeDisposable _disposables = new CompositeDisposable();

        private readonly IScheduler _scheduler;
        private readonly CommentRepository _commentRepository;
        private readonly CommentDisplayingRangeExtractor _commentDisplayingRangeExtractor;
        private readonly CommentFiltering _commentFiltering;
        private INiconicoCommentSessionProvider _niconicoCommentSessionProvider;
        private readonly MediaPlayer _mediaPlayer;

        public ObservableRangeCollection<Comment> Comments { get; private set; }
        public ObservableRangeCollection<Comment> DisplayingComments { get; } = new ObservableRangeCollection<Comment>();
        public ReactiveProperty<string> WritingComment { get; private set; }
        public ReactiveProperty<string> CommandText { get; private set; }
        public ReactiveProperty<bool> NowSubmittingComment { get; private set; }

        public AsyncReactiveCommand CommentSubmitCommand { get; }
        ICommentSession _commentSession;

        Models.Helpers.AsyncLock _commentUpdateLock = new Models.Helpers.AsyncLock();

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
            Repository.NicoVideo.CommentRepository commentRepository,
            UseCase.CommentDisplayingRangeExtractor commentDisplayingRangeExtractor,
            UseCase.NicoVideoPlayer.CommentFiltering commentFiltering
            )
        {
            _mediaPlayer = mediaPlayer;
            _scheduler = scheduler;
            _commentRepository = commentRepository;
            _commentDisplayingRangeExtractor = commentDisplayingRangeExtractor;
            _commentFiltering = commentFiltering;

            Comments = new ObservableRangeCollection<Comment>();

            CommentSubmitCommand = new AsyncReactiveCommand()
                .AddTo(_disposables);

            CommentSubmitCommand.Subscribe(async _ => await SubmitComment())
                .AddTo(_disposables);

            WritingComment = new ReactiveProperty<string>(_scheduler, string.Empty)
                .AddTo(_disposables);

            NowSubmittingComment = new ReactiveProperty<bool>(_scheduler)
                .AddTo(_disposables);


            CommandText = new ReactiveProperty<string>(_scheduler, string.Empty)
                .AddTo(_disposables);

            new[]
            {
                Observable.FromEventPattern<CommentFiltering.CommentOwnerIdFilteredEventArgs>(
                    h => _commentFiltering.FilteringCommentOwnerIdAdded += h,
                    h => _commentFiltering.FilteringCommentOwnerIdAdded -= h
                ).ToUnit(),
                Observable.FromEventPattern<CommentFiltering.CommentOwnerIdFilteredEventArgs>(
                    h => _commentFiltering.FilteringCommentOwnerIdRemoved += h,
                    h => _commentFiltering.FilteringCommentOwnerIdRemoved -= h
                ).ToUnit(),
                Observable.FromEventPattern<CommentFiltering.FilteringCommentTextKeywordEventArgs>(
                    h => _commentFiltering.FilterKeywordAdded += h,
                    h => _commentFiltering.FilterKeywordAdded -= h
                ).ToUnit(),
                Observable.FromEventPattern<CommentFiltering.FilteringCommentTextKeywordEventArgs>(
                    h => _commentFiltering.FilterKeywordRemoved += h,
                    h => _commentFiltering.FilterKeywordRemoved -= h
                ).ToUnit(),
                Observable.FromEventPattern<CommentFiltering.FilteringCommentTextKeywordEventArgs>(
                    h => _commentFiltering.FilterKeywordUpdated += h,
                    h => _commentFiltering.FilterKeywordUpdated -= h
                ).ToUnit(),
            }
            .Merge()
            .Subscribe(_ => RefreshFiltering())
            .AddTo(_disposables);
            
        }

        void RefreshFiltering()
        {
            FilteredCommentIds.Clear();
            
            var displayingComments = _commentDisplayingRangeExtractor.Rewind(_mediaPlayer.PlaybackSession.Position);

            DisplayingComments.Clear();
            foreach (var comment in FilteringComment(displayingComments.ToArray()))
            {
                DisplayingComments.Add(comment);
            }
        }

        private void RewindAsync(TimeSpan position)
        {
            DisplayingComments.Clear();
            var displayingComments = _commentDisplayingRangeExtractor.Rewind(position);
            foreach (var comment in FilteringComment(displayingComments.ToArray()))
            {
                DisplayingComments.Add(comment);
            }
        }



        public void Dispose()
        {
            _mediaPlayer.PlaybackSession.SeekCompleted -= PlaybackSession_SeekCompleted;
            _mediaPlayer.PlaybackSession.PositionChanged -= PlaybackSession_PositionChanged;
            ClearCurrentSession();

            _disposables.Dispose();
        }


        public void ClearCurrentSession()
        {
            _mediaPlayer.PlaybackSession.SeekCompleted -= PlaybackSession_SeekCompleted;
            _mediaPlayer.PlaybackSession.PositionChanged -= PlaybackSession_PositionChanged;

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


        public async Task UpdatePlayingCommentAsync(INiconicoCommentSessionProvider niconicoCommentSessionProvider)
        {
            using (await _commentUpdateLock.LockAsync())
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

                    _mediaPlayer.PlaybackSession.PositionChanged -= PlaybackSession_PositionChanged;
                    _mediaPlayer.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;
                    _mediaPlayer.PlaybackSession.SeekCompleted -= PlaybackSession_SeekCompleted;
                    _mediaPlayer.PlaybackSession.SeekCompleted += PlaybackSession_SeekCompleted;

                }
                catch
                {

                }
            }
        }

        private void PlaybackSession_SeekCompleted(MediaPlaybackSession sender, object args)
        {
            CurrentCommentIndex = 0;
            CurrentComment = null;

            TickNextDisplayingComments(sender.Position);
            UpdateNicoScriptComment(sender.Position);
            RefreshCurrentPlaybackPositionComment(sender.Position);

            _PrevPlaybackPosition = sender.Position;
        }

        public async Task RefreshComments()
        {
            if (_commentSession == null) { return; }

            using (await _commentUpdateLock.LockAsync())
            {
                await UpdateComments_Internal(_commentSession);
            }
        }

        

        private async Task SubmitComment()
        {
            using (await _commentUpdateLock.LockAsync())
            {
                if (_commentSession == null) { return; }

                NowSubmittingComment.Value = true;

                Debug.WriteLine($"try comment submit:{WritingComment.Value}");

                var posision = _mediaPlayer.PlaybackSession.Position;

                try
                {
                    var vpos = (uint)(posision.TotalMilliseconds / 10);

                    var res = await _commentSession.PostComment(WritingComment.Value, posision, CommandText.Value);

                    if (res.Status == ChatResult.Success)
                    {
                        Debug.WriteLine("コメントの投稿に成功: " + res.CommentNo);

                        var commentVM = new Comment()
                        {
                            CommentId = (uint)res.CommentNo,
                            VideoPosition = vpos,
                            UserId = _commentSession.UserId,
                            CommentText = WritingComment.Value,
                        };

                        Comments.Add(commentVM);
                        
                        ResetDisplayingComments(Comments);

                        WritingComment.Value = "";
                    }
                    else
                    {
                        CommentSubmitFailed?.Invoke(this, EventArgs.Empty);

                        //                    _NotificationService.ShowToast("コメント投稿", $"{_commentSession.ContentId} へのコメント投稿に失敗 （error code : {res.StatusCode}", duration: Microsoft.Toolkit.Uwp.Notifications.ToastDuration.Short);

                        Debug.WriteLine("コメントの投稿に失敗: " + res.Status.ToString());
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
        }




        private async Task UpdateComments_Internal(ICommentSession commentSession)
        {
            // ニコスクリプトの状態を初期化
            ClearNicoScriptState();

            Comments.Clear();
            DisplayingComments.Clear();

            var comments = await commentSession.GetInitialComments();

            IEnumerable<Comment> commentsAction(IEnumerable<Comment> comments)
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

            Comments.AddRange(commentsAction(comments.OrderBy(x => x.VideoPosition)));

            if (commentSession is VideoCommentService onlineCommentSession)
            {
                _ = Task.Run(() =>
                {
                    _commentRepository.SetCache(commentSession.ContentId, comments);
                });
            }

            ResetDisplayingComments(Comments);

            _NicoScriptList.Sort((x, y) => (int)(x.BeginTime.Ticks - y.BeginTime.Ticks));
            System.Diagnostics.Debug.WriteLine($"コメント数:{Comments.Count}");
            
        }


        void ResetDisplayingComments(IReadOnlyCollection<Comment> comments)
        {
            DisplayingComments.Clear();
            Debug.WriteLine($"CommentReset");

            var displayingComments = _commentDisplayingRangeExtractor.ResetComments(comments, _mediaPlayer.PlaybackSession.Position);
            DisplayingComments.AddRange(FilteringComment(displayingComments.ToArray()));
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
                DisplayingComments.AddRange(FilteringComment(comments.AddedComments.ToArray()));
            }
        }

        bool FilteringComment(Comment comment)
        {
            bool IsFilteredComment(Comment comment)
            {
                if (_commentFiltering.IsCommentOwnerUserIdFiltered(comment.UserId))
                {
                    return true;
                }
                else if (_commentFiltering.GetAllFilteringCommentTextCondition().IsMatchAny(comment.CommentText))
                {
                    return true;
                }

                return false;
            }

            if (IsFilteredComment(comment))
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

                foreach (var action in MailToCommandHelper.MakeCommandActions(commands))
                {
                    action(comment);
                }
            }

            comment.CommentText = _commentFiltering.TransformCommentText(comment.CommentText);

            return false;
        }

        bool FilteringCommentWithCache(Comment comment)
        {
            if (FilteredCommentIds.TryGetValue(comment.CommentId, out bool isFiltered))
            {
                return !isFiltered;
            }
            else
            {
                var isFilteredComment = FilteringComment(comment);
                FilteredCommentIds.Add(comment.CommentId, isFilteredComment);
                return !isFilteredComment;
            }
        }

        IEnumerable<Comment> FilteringComment(IEnumerable<Comment> comments)
        {
            foreach (var comment in comments)
            {
                if (FilteringCommentWithCache(comment))
                {
                    yield return comment;
                }
            }
        }


        Dictionary<uint, bool> FilteredCommentIds = new Dictionary<uint, bool>();

        TimeSpan _PrevPlaybackPosition;
        private void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
        {
            TickNextDisplayingComments(sender.Position);
            UpdateNicoScriptComment(sender.Position);
            RefreshCurrentPlaybackPositionComment(sender.Position);

            _PrevPlaybackPosition = sender.Position;
        }

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
            const int CommentReadabilityIncreaseDelayVpos = 0;
            var vpos = (long)(position.TotalMilliseconds * 0.1) + CommentReadabilityIncreaseDelayVpos;
            var currentIndex = CurrentCommentIndex;
            foreach (var comment in Comments.Skip(CurrentCommentIndex).Cast<Comment>())
            {
                if ((comment as Comment).VideoPosition > vpos)
                {
                    CurrentComment = comment;
                    break;
                }

                ++currentIndex;
            }

            CurrentCommentIndex = currentIndex;
        }

        private Comment _CurrentComment;

        [PropertyChanged.DoNotNotify]
        public Comment CurrentComment
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
        
        private static bool IsNicoScriptComment(string userId, string content)
        {
            if (userId != null) { return false; }

            if (string.IsNullOrEmpty(content)) { return false; }

            return (content.StartsWith("＠") || content.StartsWith("@") || content.StartsWith("/"));
        }



        private bool TryAddNicoScript(Comment chat)
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
            var beginTime = TimeSpan.FromMilliseconds(chat.VideoPosition * 10);
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
                        else if (NiconicoRegex.IsVideoId(condition))
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
