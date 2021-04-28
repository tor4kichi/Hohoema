using Mntone.Nico2.Videos.Comment;
using Hohoema.Models.Domain;
using Hohoema.Models.Helpers;
using Hohoema.Presentation.ViewModels;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;

namespace Hohoema.Models.Domain.Player.Video.Comment
{

    [DataContract]
	public class VideoComment : BindableBase, IComment
    {
        // コメントのデータ構造だけで他のことを知っているべきじゃない
        // このデータを解釈して実際に表示するためのオブジェクトにする部分は処理は
        // View側が持つべき
        
        [DataMember]
        public uint CommentId { get; set; }

        [DataMember]
        public string CommentText { get; set; }

        [DataMember]
        public string Mail { get; set; }
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

            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(text);

            var root = doc.DocumentNode;
            var anchorNode = root.Descendants("a").FirstOrDefault();
            if (anchorNode != null)
            {
                if (anchorNode.Attributes.Contains("href"))
                {
                    return new Uri(anchorNode.Attributes["href"].Value);
                }
            }

            return null;
        }
    }
}
