﻿namespace Hohoema.Models.Repository.Niconico.NicoVideo.Comment
{
    public enum ChatResult
    {
        Success = 0,
        Failure = 1,
        InvalidThread = 2,
        InvalidTichet = 3,
        InvalidPostkey = 4,
        Locked = 5,
        Readonly = 6,
        TooLong = 8
    }

}
