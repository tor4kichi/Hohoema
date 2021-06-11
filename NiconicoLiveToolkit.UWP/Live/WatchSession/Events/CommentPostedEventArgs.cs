
namespace NiconicoToolkit.Live.WatchSession.Events
{
    public struct CommentPostedEventArgs
    {
        public string Thread { get; set; }
        public ChatResult ChatResult { get; set; }
        public int No { get; set; }
    }
}
