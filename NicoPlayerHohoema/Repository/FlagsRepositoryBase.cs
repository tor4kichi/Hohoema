using Microsoft.Toolkit.Uwp.Helpers;
using NicoPlayerHohoema.FixPrism;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Uno.Threading;
using Windows.Storage;

namespace NicoPlayerHohoema.Repository
{
    public class FlagsRepositoryBase : BindableBase
    {
        private readonly LocalObjectStorageHelper _LocalStorageHelper;
        FastAsyncLock _fileUpdateLock = new FastAsyncLock();
        public FlagsRepositoryBase()
        {
            _LocalStorageHelper = new Microsoft.Toolkit.Uwp.Helpers.LocalObjectStorageHelper();
        }

        protected T Read<T>(T @default = default, [CallerMemberName] string propertyName = null)
        {
            return _LocalStorageHelper.Read<T>(propertyName, @default);
        }

        protected async Task<T> ReadFileAsync<T>(T value, [CallerMemberName] string propertyName = null)
        {
            using (await _fileUpdateLock.LockAsync(default))
            {
                return await _LocalStorageHelper.ReadFileAsync(propertyName, value);
            }
        }

        protected void Save<T>(T value, [CallerMemberName] string propertyName = null)
        {
            _LocalStorageHelper.Save(propertyName, value);
        }

        protected async Task<StorageFile> SaveFileAsync<T>(T value, [CallerMemberName] string propertyName = null)
        {
            using (await _fileUpdateLock.LockAsync(default))
            {
                return await _LocalStorageHelper.SaveFileAsync(propertyName, value);
            }
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

        protected bool SetProperty<T>(ref T? storage, T? value, [CallerMemberName] string propertyName = null)
            where T : struct
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
