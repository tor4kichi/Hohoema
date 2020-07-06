using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Hohoema.Database
{
    [Obsolete]
    public static class HohoemaLiteDb
    {
        internal const string DbTempFileName = @"hohoema_temp.db";

        public static readonly string TempLocalDbFilePath = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, DbTempFileName);
        static readonly string TempConnectionString = $"Filename={TempLocalDbFilePath}; Async=false; upgrade=true;";

        static public void Initialize(Func<string, LiteRepository> initializer)
        {
            HohoemaTempLiteRepository = initializer(TempConnectionString);
            HohoemaLocalLiteRepository = initializer(LocalConnectionString);
        }


        static LiteRepository HohoemaTempLiteRepository;
        static internal LiteRepository GetTempLiteRepository()
        {
            return HohoemaTempLiteRepository;
        }

        static public async Task DeleteTempDbFile()
        {
            var file = await StorageFile.GetFileFromPathAsync(TempLocalDbFilePath);
            if (file != null)
            {
                await file.DeleteAsync();
            }

        }

        internal const string LocalDbFileName = @"hohoema.db";

        static readonly string LocalConnectionString = $"Filename={Path.Combine(ApplicationData.Current.LocalFolder.Path, LocalDbFileName)}; Async=false; upgrade=true;";

        static LiteRepository HohoemaLocalLiteRepository;
        static internal LiteRepository GetLocalLiteRepository()
        {
            return HohoemaLocalLiteRepository;
        }

    }
}
