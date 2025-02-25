using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Helpers;
public static class ObservableObjectDebugHelper
{    
    public static IDisposable ObservePropertiesDebugOutput(this INotifyPropertyChanged notify)
    {
#if DEBUG
        return notify.PropertyChangedAsObservable()
            .Subscribe(x =>
            {
                var propertyInfo = notify.GetType().GetProperty(x.PropertyName);
                if (propertyInfo != null)
                {
                    Debug.WriteLine($"{x.PropertyName} = {propertyInfo.GetValue(notify)}");
                }
            });
#else
        return Disposable.Empty;
#endif

    }
}
