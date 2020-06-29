using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.Niconico.NicoVideo.Comment
{
    internal static class ChatResultMapper
    {
        public static ChatResult ToModelChatResult(this Mntone.Nico2.Videos.Comment.ChatResult chatResult)
        {
            return chatResult switch
            {
                Mntone.Nico2.Videos.Comment.ChatResult.Success => ChatResult.Success,
                Mntone.Nico2.Videos.Comment.ChatResult.Failure => ChatResult.Failure,
                Mntone.Nico2.Videos.Comment.ChatResult.InvalidThread => ChatResult.InvalidThread,
                Mntone.Nico2.Videos.Comment.ChatResult.InvalidTichet => ChatResult.InvalidTichet,
                Mntone.Nico2.Videos.Comment.ChatResult.InvalidPostkey => ChatResult.InvalidPostkey,
                Mntone.Nico2.Videos.Comment.ChatResult.Locked => ChatResult.Locked,
                Mntone.Nico2.Videos.Comment.ChatResult.Readonly => ChatResult.Readonly,
                Mntone.Nico2.Videos.Comment.ChatResult.TooLong => ChatResult.TooLong,
                _ => throw new NotSupportedException()
            };
        }

        public static Mntone.Nico2.Videos.Comment.ChatResult ToInfrastructureChatResult(this ChatResult chatResult)
        {
            return chatResult switch
            {
                ChatResult.Success => Mntone.Nico2.Videos.Comment.ChatResult.Success,
                ChatResult.Failure => Mntone.Nico2.Videos.Comment.ChatResult.Failure,
                ChatResult.InvalidThread => Mntone.Nico2.Videos.Comment.ChatResult.InvalidThread,
                ChatResult.InvalidTichet => Mntone.Nico2.Videos.Comment.ChatResult.InvalidTichet,
                ChatResult.InvalidPostkey => Mntone.Nico2.Videos.Comment.ChatResult.InvalidPostkey,
                ChatResult.Locked => Mntone.Nico2.Videos.Comment.ChatResult.Locked,
                ChatResult.Readonly => Mntone.Nico2.Videos.Comment.ChatResult.Readonly,
                ChatResult.TooLong => Mntone.Nico2.Videos.Comment.ChatResult.TooLong,
                _ => throw new NotSupportedException()
            };
        }
    }
}
