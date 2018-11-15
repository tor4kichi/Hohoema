using System;
using System.Threading.Tasks;
using Mntone.Nico2;

namespace NicoPlayerHohoema.Models.Live
{
    public interface INicoLiveCommentClient
    {
        bool IsConnected { get; }

        event EventHandler<CommentPostedEventArgs> CommentPosted;
        event EventHandler<CommentRecievedEventArgs> CommentRecieved;
        event EventHandler<CommentServerConnectedEventArgs> Connected;
        event EventHandler<CommentServerDisconnectedEventArgs> Disconnected;


        void Open();
        void Close();

        void Seek(TimeSpan timeSpanFromOpenTime);

        void PostComment(string comment, string command, string postKey, TimeSpan elapsedTime);
    }
}