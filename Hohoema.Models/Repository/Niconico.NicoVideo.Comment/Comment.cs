using Mntone.Nico2.Videos.Comment;
using Hohoema.Models;
using Hohoema.Models.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;

namespace Hohoema.Models.Repository.Niconico.NicoVideo.Comment
{


    [DataContract]
    public class Comment : IComment
    {
        [DataMember]
        public uint CommentId { get; set; }

        [DataMember]
        public string CommentText { get; set; }

        [DataMember]
        public string Mail { get; set; }
        [DataMember]
        public string UserId { get; set; }

        [DataMember]
        public long VideoPosition { get; set; }

        [DataMember]
        public int NGScore { get; set; }

        [DataMember]
        public int DeletedFlag { get; set; }

        public bool IsDeleted => DeletedFlag != 0;

        TimeSpan VideoPositionTS => TimeSpan.FromMilliseconds(VideoPosition * 10);
    }



}
