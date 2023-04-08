using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Contracts.Services.Navigations
{
    public interface IViewLocator
    {
        Type ResolveView(string viewName);
    }
}
