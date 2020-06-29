using Microsoft.Toolkit.Uwp.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository
{
    public class FlagsRepositoryBase : FixPrism.BindableBase
    {
        private readonly LocalObjectStorageHelper _LocalStorageHelper;

        public FlagsRepositoryBase()
        {
            _LocalStorageHelper = new Microsoft.Toolkit.Uwp.Helpers.LocalObjectStorageHelper();
        }

        protected T Read<T>(T @default = default, [CallerMemberName] string propertyName = null)
        {
            return _LocalStorageHelper.Read<T>(propertyName, @default);
        }

        protected void Save<T>(T value, [CallerMemberName] string propertyName = null)
        {
            _LocalStorageHelper.Save(propertyName, value);
        }

        protected void Save<T>(T? value, [CallerMemberName] string propertyName = null)
            where T : struct
        {
            _LocalStorageHelper.Save(propertyName, value);
        }

        protected override bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (base.SetProperty(ref storage, value, propertyName))
            {
                Save<T>(value, propertyName);
                return true;
            }
            else
            {
                return true;
            }
        }

        protected override bool SetProperty<T>(ref T? storage, T? value, [CallerMemberName] string propertyName = null)
        {
            if (base.SetProperty(ref storage, value, propertyName))
            {
                Save<T>(value, propertyName);
                return true;
            }
            else
            {
                return true;
            }
        }

    }
}
