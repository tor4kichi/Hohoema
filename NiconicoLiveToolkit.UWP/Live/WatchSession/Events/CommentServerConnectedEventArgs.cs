namespace NiconicoToolkit.Live.WatchSession.Events
{
    public struct CommentServerConnectedEventArgs
    {
        public int Resultcode { get; set; }
        public string Thread { get; set; }
        public int ServerTime { get; set; }
        public int LastRes { get; set; }
        public string Ticket { get; set; }
        public int Revision { get; set; }
    }
}
