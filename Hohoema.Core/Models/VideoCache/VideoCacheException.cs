using System;
using System.Runtime.Serialization;

namespace Hohoema.Models.VideoCache
{
    public sealed class VideoCacheException : Exception
    {
        public VideoCacheException()
        {
        }

        public VideoCacheException(string message) : base(message)
        {
        }

        public VideoCacheException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public VideoCacheException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
