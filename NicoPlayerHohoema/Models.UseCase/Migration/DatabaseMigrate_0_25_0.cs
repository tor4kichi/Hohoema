using Hohoema.Models.Domain.Application;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Hohoema.Models.UseCase.Migration
{
    public sealed class DatabaseMigrate_0_25_0 : IMigrateAsync
    {
        private readonly AppFlagsRepository _appFlagsRepository;

        public DatabaseMigrate_0_25_0(AppFlagsRepository appFlagsRepository)
        {
            _appFlagsRepository = appFlagsRepository;
        }

        public async Task MigrateAsync()
        {
            // 既存のhohoema.dbを削除して_v3をhohoema.dbに上書きする
            if (_appFlagsRepository.IsDatabaseMigration_V_0_25_0) { return; }
            try
            {
                var oldDbFile = await ApplicationData.Current.LocalFolder.GetFileAsync("_v3");
                if (oldDbFile != null)
                {
                    await oldDbFile.RenameAsync("hohoema.db", NameCollisionOption.ReplaceExisting);
                }

                var barrel = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appdata:///local/file.txt"));
            }
            catch { }
            finally
            {
                _appFlagsRepository.IsDatabaseMigration_V_0_25_0 = true;
            }
        }
    }
}
