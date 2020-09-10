using Hohoema.Models.Domain.Application;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Hohoema.Models.UseCase.Migration
{
    public sealed class DatabaseMigration_V_0_23_0 : IMigrateAsync
    {
        private readonly AppFlagsRepository _appFlagsRepository;

        public DatabaseMigration_V_0_23_0(AppFlagsRepository appFlagsRepository)
        {
            _appFlagsRepository = appFlagsRepository;
        }

        public async Task MigrateAsync()
        {
            if (_appFlagsRepository.IsDatabaseMigration_V_0_23_0) { return; }

            try
            {
                var file = await ApplicationData.Current.LocalFolder.GetFileAsync("_v3");
                if (file != null)
                {
                    await file.RenameAsync("hohoema.db");
                }
            }
            catch { }

            _appFlagsRepository.IsDatabaseMigration_V_0_23_0 = true;
        }
    }
}
