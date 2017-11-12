using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace NicoPlayerHohoema.Database
{
    public static class HohoemaLiteDb
    {
        internal const string DbFileName = @"hohoema_local.db";

        static readonly string ConnectionString = $"Filename={Path.Combine(ApplicationData.Current.TemporaryFolder.Path, DbFileName)}; Async=false;";

        static LiteRepository HohoemaLocalLiteRepository = new LiteRepository(ConnectionString);
        static internal LiteRepository GetLiteRepository()
        {
            return HohoemaLocalLiteRepository;
        }
    }
}
