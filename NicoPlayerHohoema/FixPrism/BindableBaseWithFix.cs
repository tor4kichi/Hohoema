using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Prism.Mvvm;

namespace NicoPlayerHohoema.FixPrism
{
    public class BindableBaseWithFix : BindableBase
    {
        protected virtual bool SetProperty<T>(ref T? storage, T? value, [CallerMemberName] string propertyName = null)
            where T : struct
        {
            if (EqualityComparer<T?>.Default.Equals(storage, value))
                return false;
            storage = value;
            RaisePropertyChanged(propertyName);
            return true;
        }
    }
}
