using System.IO;
using Windows.Storage;

namespace Hohoema.Database;

public static class HohoemaLiteDb
{
    internal const string DbTempFileName = @"hohoema_local.db";

    static readonly string TempConnectionString = $"Filename={Path.Combine(ApplicationData.Current.TemporaryFolder.Path, DbTempFileName)}; Async=false;";



    internal const string LocalDbFileName = @"hohoema.db";

    static readonly string LocalConnectionString = $"Filename={Path.Combine(ApplicationData.Current.LocalFolder.Path, LocalDbFileName)}; Async=false;";
}
