#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using Hohoema.Helpers;
using Microsoft.Toolkit.Uwp.Helpers;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Storage;

namespace Hohoema.Infra;

public class FlagsRepositoryBase : ObservableObject
{
    [System.Obsolete]
    private readonly LocalObjectStorageHelper _LocalStorageHelper;
    private readonly AsyncLock _fileUpdateLock = new();

    [System.Obsolete]
    public FlagsRepositoryBase()
    {
        _LocalStorageHelper = new LocalObjectStorageHelper(new SystemTextJsonSerializer());
    }

    [System.Obsolete]
    protected T Read<T>(T @default = default, [CallerMemberName] string propertyName = null)
    {
        return _LocalStorageHelper.Read<T>(propertyName, @default);
    }

    [System.Obsolete]
    protected async Task<T> ReadFileAsync<T>(T value, [CallerMemberName] string propertyName = null)
    {
        using (await _fileUpdateLock.LockAsync(default))
        {
            return await _LocalStorageHelper.ReadFileAsync(propertyName, value);
        }
    }

    [System.Obsolete]
    protected void Save<T>(T value, [CallerMemberName] string propertyName = null)
    {
        _LocalStorageHelper.Save(propertyName, value);
    }

    [System.Obsolete]
    protected async Task<StorageFile> SaveFileAsync<T>(T value, [CallerMemberName] string propertyName = null)
    {
        using (await _fileUpdateLock.LockAsync(default))
        {
            return await _LocalStorageHelper.SaveFileAsync(propertyName, value);
        }
    }

    [System.Obsolete]
    protected void Save<T>(T? value, [CallerMemberName] string propertyName = null)
        where T : struct
    {
        _LocalStorageHelper.Save(propertyName, value);
    }

    [System.Obsolete]
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

    [System.Obsolete]
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
