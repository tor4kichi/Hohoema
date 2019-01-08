using Mntone.Nico2.Videos.Comment;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Models.Helpers;
using NicoPlayerHohoema.ViewModels;
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

namespace NicoPlayerHohoema.Models.Niconico
{
    public enum CommentDisplayMode
    {
        Scrolling,
        Top,
        Center,
        Bottom,
    }

    public enum CommentSizeMode
    {
        Normal,
        Big,
        Small,
    }

	public class Comment : BindableBase
	{
        // コメントのデータ構造だけで他のことを知っているべきじゃない
        // このデータを解釈して実際に表示するためのオブジェクトにする部分は処理は
        // View側が持つべき
        /*
		public const float default_fontSize = fontSize_mid;

		public const float fontSize_mid = 1.0f;
		public const float fontSize_small = 0.75f;
		public const float fontSize_big = 1.25f;
        */
        public uint CommentId { get; set; }

        public string CommentText { get; set; }
        public string Mail { get; set; }
		public string UserId { get; set; }
        public bool IsAnonimity { get; set; }

        public long VideoPosition { get; set; }

        public int NGScore { get; set; }


        public bool IsLoginUserComment { get; set; }
        public bool IsOwnerComment { get; set; }

        public int DeletedFlag { get; set; }

        




        public CommentDisplayMode DisplayMode { get; set; }

        public bool IsScrolling => DisplayMode == CommentDisplayMode.Scrolling;


        public CommentSizeMode SizeMode { get; set; }

        public bool IsInvisible { get; set; }


        public Color? Color { get; set; }



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
