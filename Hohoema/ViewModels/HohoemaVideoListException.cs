using Hohoema.Models.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
