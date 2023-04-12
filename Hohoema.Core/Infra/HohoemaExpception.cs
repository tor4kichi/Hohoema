#nullable enable
using System;
using System.Runtime.Serialization;

namespace Hohoema.Infra;

public class HohoemaException : Exception
{
    public HohoemaException()
    {
    }

    public HohoemaException(string message) : base(message)
    {
    }

    public HohoemaException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public HohoemaException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}
