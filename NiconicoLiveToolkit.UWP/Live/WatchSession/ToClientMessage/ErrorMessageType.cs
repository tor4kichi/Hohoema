using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace NiconicoToolkit.Live.WatchSession
{
    public enum ErrorMessageType
    {
        INVALID_MESSAGE,
        CONNECT_ERROR,
        CONTENT_NOT_READY,
        INVALID_STREAM_QUALITY,
        NO_THREAD_AVAILABLE,
        NO_ROOM_AVAILABLE,
        NO_PERMISSION,
        NOT_ON_AIR,
        COMMENT_LOCKED,
        NO_STREAM_AVAILABLE,
        BROADCAST_NOT_FOUND,
        INTERNAL_SERVERERROR,
    }
}
