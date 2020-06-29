namespace Hohoema.Models.Repository.Niconico.NicoVideo.Histories
{
    public sealed class RemoveHistoryResponse
    {
        private readonly Mntone.Nico2.Videos.RemoveHistory.RemoveHistoryResponse _res;

        internal RemoveHistoryResponse(Mntone.Nico2.Videos.RemoveHistory.RemoveHistoryResponse res)
        {
            _res = res;
        }


        public bool IsOK => _res.IsOK;

        /// <summary>
        /// 履歴の件数
        /// </summary>
        public ushort HistoryCount => _res.HistoryCount;

        /// <summary>
        /// 削除した動画の ID
        /// </summary>
        public string RemovedId => _res.RemovedId;
    }

}
