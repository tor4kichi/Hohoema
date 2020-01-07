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

namespace NicoPlayerHohoema.UseCase
{
    public class CommentPlayer : BindableBase, IDisposable
    {
        CompositeDisposable _disposables = new CompositeDisposable();

        private readonly IScheduler _scheduler;
        private readonly CommentRepository _commentRepository;
        private INiconicoCommentSessionProvider _niconicoCommentSessionProvider;
        private readonly MediaPlayer _mediaPlayer;

        public ObservableCollection<Comment> Comments { get; private set; }
        public ReactiveProperty<string> WritingComment { get; private set; }
        public ReactiveProperty<bool> NowSubmittingComment { get; private set; }

        public CommentCommandEditerViewModel CommandEditer { get;}

        public AsyncReactiveCommand CommentSubmitCommand { get; }
        ICommentSession _commentSession;

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
            CommentCommandEditerViewModel commandEditer,
            Repository.NicoVideo.CommentRepository commentRepository
            )
        {
            _mediaPlayer = mediaPlayer;
            _scheduler = scheduler;
            CommandEditer = commandEditer;
            _commentRepository = commentRepository;
            mediaPlayer.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;

            Comments = new ObservableCollection<Comment>();

            CommentSubmitCommand = new AsyncReactiveCommand()
                .AddTo(_disposables);

            CommentSubmitCommand.Subscribe(async _ => await SubmitComment())
                .AddTo(_disposables);

            WritingComment = new ReactiveProperty<string>(_scheduler, "")
                .AddTo(_disposables);

            NowSubmittingComment = new ReactiveProperty<bool>(_scheduler)
                .AddTo(_disposables);
        }



        public void Dispose()
        {
            ClearCurrentSession();

            _disposables.Dispose();
        }


        public void ClearCurrentSession()
        {
            _commentSession?.Dispose();
            _commentSession = null;
            
            _niconicoCommentSessionProvider = null;

            ClearNicoScriptState();

            Comments.Clear();

            WritingComment.Value = string.Empty;
        }


        public async Task UpdatePlayingCommentAsync(INiconicoCommentSessionProvider niconicoCommentSessionProvider)
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
            }
            catch
            {

            }
        }


        public async Task RefreshComments()
        {
            if (_commentSession == null) { return; }

            await UpdateComments_Internal(_commentSession);
        }

        

        private async Task SubmitComment()
        {
            if (_commentSession == null) { return; }

            NowSubmittingComment.Value = true;

            Debug.WriteLine($"try comment submit:{WritingComment.Value}");

            var posision = _mediaPlayer.PlaybackSession.Position;

            try
            {
                var vpos = (uint)(posision.TotalMilliseconds / 10);

                var res = await _commentSession.PostComment(WritingComment.Value, posision, CommandEditer.MakeCommands());

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

                    if (CommandEditer.IsPickedColor.Value)
                    {
                        var color = CommandEditer.FreePickedColor.Value;
                        commentVM.Color = color;
                    }

                    Comments.Add(commentVM);

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




        private async Task UpdateComments_Internal(ICommentSession commentSession)
        {
            Comments.Clear();


            var comments = await commentSession.GetInitialComments();


            // 投コメからニコスクリプトをセットアップしていく
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

                // コメントをコメントリストに追加する（通常の投コメも含めて）
                Comments.Add(comment);
            }

            // ニコスクリプトの状態を初期化
            ClearNicoScriptState();
            _NicoScriptList.Sort((x, y) => (int)(x.BeginTime.Ticks - y.BeginTime.Ticks));

            System.Diagnostics.Debug.WriteLine($"コメント数:{Comments.Count}");

            if (commentSession is VideoCommentService onlineCommentSession)
            {
                _ = Task.Run(() =>
                {
                    _commentRepository.SetCache(commentSession.ContentId, comments);                    
                });
            }
        }

        TimeSpan _PrevPlaybackPosition;
        private void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
        {
            foreach (var script in _NicoScriptList)
            {
                if (_PrevPlaybackPosition <= script.BeginTime && sender.Position > script.BeginTime)
                {
                    if (script.EndTime < sender.Position)
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

                    if (_PrevPlaybackPosition < script.EndTime && sender.Position > script.EndTime)
                    {
                        Debug.WriteLine("nicoscript Disabling :" + script.Type);
                        script.ScriptDisabling?.Invoke();
                    }
                }
            }

            _PrevPlaybackPosition = sender.Position;
        }


        #region 



        List<NicoScript> _NicoScriptList = new List<NicoScript>();
        List<ReplaceNicoScript> _ReplaceNicoScirptList = new List<ReplaceNicoScript>();
        List<DefaultCommandNicoScript> _DefaultCommandNicoScriptList = new List<DefaultCommandNicoScript>();
        
        private static bool IsNicoScriptComment(string userId, string content)
        {
            return userId == null && (content.StartsWith("＠") || content.StartsWith("@") || content.StartsWith("/"));
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
