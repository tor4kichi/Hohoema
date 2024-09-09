using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using R3;

namespace Hohoema.ViewModels.Player;
public partial class LivePlayerPageViewModel
{
    public IDisposable Subscribe<T>(Observable<T> observable, Action<T> action)
    {
        return observable.Subscribe(action);
    }
}
