using Microsoft.Toolkit.Uwp.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using System.Runtime.Serialization.Json;
using Hohoema.Models.Helpers;

namespace Hohoema.Models.Infrastructure
{

    public class FlagsRepositoryBase : ObservableObject
    {
        private readonly LocalObjectStorageHelper _LocalStorageHelper;
        AsyncLock _fileUpdateLock = new AsyncLock();
        public FlagsRepositoryBase()
        {
            _LocalStorageHelper = new LocalObjectStorageHelper(new SystemTextJsonSerializer());
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

        protected new bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (base.SetProperty(ref storage, value, propertyName))
            {
                Save<T>(value, propertyName);
                return true;
            }
            else
            {
                return false;
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
