using Hohoema.Models.Domain;
using Hohoema.Models.Helpers;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;
using AngleSharp.Html.Parser;
using Hohoema.Models.Domain.Player.Comment;
using NiconicoToolkit.Video.Watch.NV_Comment;
using Hohoema.Models.Domain.Niconico.Video;

namespace Hohoema.Models.Domain.Player.Video.Comment
{   
    public interface IVideoComment : IComment
    {
        
    }

    [DataContract]
	public class VideoComment : ObservableObject, IVideoComment
    {
        // コメントのデータ構造だけで他のことを知っているべきじゃない
        // このデータを解釈して実際に表示するためのオブジェクトにする部分は処理は
        // View側が持つべき
        
        [DataMember]
        public uint CommentId { get; set; }

        [DataMember]
        public string CommentText { get; set; }

        [DataMember]
        public IReadOnlyList<string> Commands { get; set; }
        [DataMember]
        public string UserId { get; set; }

        [DataMember]
        public bool IsAnonymity { get; set; }

        [DataMember]
        public TimeSpan VideoPosition { get; set; }

        [DataMember]
        public int NGScore { get; set; }

        [IgnoreDataMember]
        public bool IsLoginUserComment { get; set; }

        [IgnoreDataMember]
        public bool IsOwnerComment { get; set; }

        [DataMember]
        public int DeletedFlag { get; set; }


        private string _commentText_Transformed;
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
    }


    public class LiveComment : VideoComment
    {
        private string _UserName;
        public string UserName
        {
            get { return _UserName; }
            set { SetProperty(ref _UserName, value); }
        }

        private string _IconUrl;
        public string IconUrl
        {
            get { return _IconUrl; }
            set { SetProperty(ref _IconUrl, value); }
        }

        public bool IsOperationCommand { get; internal set; }

        public string OperatorCommandType { get; set; }
        public string[] OperatorCommandParameters { get; set; }

        bool? _IsLink;
        public bool IsLink
        {
            get
            {
                ResetLink();

                return _IsLink.Value;
            }
        }

        Uri _Link;
        public Uri Link
        {
            get
            {
                ResetLink();

                return _Link;
            }
        }

        private void ResetLink()
        {
            if (!_IsLink.HasValue)
            {
                if (Uri.IsWellFormedUriString(CommentText, UriKind.Absolute))
                {
                    _Link = new Uri(CommentText);
                }
                else
                {
                    _Link = ParseLinkFromHtml(CommentText);
                }

                _IsLink = _Link != null;
            }
        }


        static Uri ParseLinkFromHtml(string text)
        {
            if (text == null) { return null; }

            HtmlParser htmlParser = new HtmlParser();
            using var document = htmlParser.ParseDocument(text);

            var anchorNode = document.QuerySelector("a");
            if (anchorNode != null)
            {
                if (anchorNode.GetAttribute("href") is not null and var href)
                {
                    return new Uri(href);
                }
            }

            return null;
        }
    }
}
