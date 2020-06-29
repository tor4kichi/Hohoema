using Hohoema.Models.Niconico;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.UseCase.NicoLive
{
    public class LiveCommentViewModel : FixPrism.BindableBase
    {
        public uint CommentId { get; set; }

        public string CommentText { get; set; }

        public string Mail { get; set; }
        public string UserId { get; set; }

        public long VideoPosition { get; set; }

        public int NGScore { get; set; }

        public int DeletedFlag { get; set; }

        public bool IsDeleted => DeletedFlag != 0;


        public bool IsAnonimity { get; set; }
        public bool IsOwnerComment { get; set; }

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
