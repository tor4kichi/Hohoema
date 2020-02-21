using Microsoft.Toolkit.Uwp.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Repository
{
    public sealed class AppFlagsRepository
    {
        private readonly LocalObjectStorageHelper _LocalStorageHelper;

        public AppFlagsRepository()
        {
            _LocalStorageHelper = new Microsoft.Toolkit.Uwp.Helpers.LocalObjectStorageHelper();
        }

        private T Read<T>([CallerMemberName] string propertyName = null)
        {
            return _LocalStorageHelper.Read<T>(propertyName);
        }

        private void Save<T>(T value, [CallerMemberName] string propertyName = null)
        {
            _LocalStorageHelper.Save(propertyName, value);
        }


        public bool IsRankingInitialUpdate
        {
            get => Read<bool>();
            set => Save(value);
        }

        public bool IsMigratedCacheFolder_V_0_21_0
        {
            get => Read<bool>();
            set => Save(value);
        }
    }
}
