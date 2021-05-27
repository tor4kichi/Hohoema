namespace NiconicoToolkit.Live.WatchSession.ToClientMessage
{
    public enum DisconnectReasonType
    {
        /// <summary>
        /// 追い出された
        /// </summary>
        TAKEOVER,

        /// <summary>
        /// 座席を取れなかった
        /// </summary>
        NO_PERMISSION,

        /// <summary>
        /// 番組が終了した
        /// </summary>
        END_PROGRAM,

        /// <summary>
        /// 接続生存確認に失敗した
        /// </summary>
        PING_TIMEOUT,

        /// <summary>
        /// 同一ユーザからの接続数上限を越えている
        /// </summary>
        TOO_MANY_CONNECTIONS,

        /// <summary>
        /// 同一ユーザの視聴番組数上限を越えている
        /// </summary>
        TOO_MANY_WATCHINGS,

        /// <summary>
        /// 満席
        /// </summary>
        CROWDED,

        /// <summary>
        /// メンテナンス中
        /// </summary>
        MAINTENANCE_IN,

        /// <summary>
        /// 上記以外の一時的なサーバエラー
        /// </summary>
        SERVICE_TEMPORARILY_UNAVAILABLE,
    }

}
