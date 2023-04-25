using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Contracts.AppLifecycle;
public interface ISuspendAndResumeAware
{
    ValueTask OnSuspendingAsync();
    ValueTask OnResumingAsync();
}
