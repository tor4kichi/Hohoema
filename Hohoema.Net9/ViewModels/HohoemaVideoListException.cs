#nullable enable
using Hohoema.Infra;
using System;

namespace Hohoema.ViewModels;

internal class HohoemaVideoListException : HohoemaException
{
    public HohoemaVideoListException(string pageName, string pageParameters)
        : base($"\n{pageName}/{pageParameters}\n")
    {
    }

    public HohoemaVideoListException(string pageName, string pageParameters, Exception innerException)
        : base ($"\n{pageName}/{pageParameters}\n", innerException)
    {
    }
}
