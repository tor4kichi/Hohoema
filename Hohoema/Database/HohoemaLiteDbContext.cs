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
    public static class HohoemaLiteDb
    {
        internal const string DbTempFileName = @"hohoema_local.db";

        static readonly string TempConnectionString = $"Filename={Path.Combine(ApplicationData.Current.TemporaryFolder.Path, DbTempFileName)}; Async=false;";



        internal const string LocalDbFileName = @"hohoema.db";

        static readonly string LocalConnectionString = $"Filename={Path.Combine(ApplicationData.Current.LocalFolder.Path, LocalDbFileName)}; Async=false;";
    }
}
