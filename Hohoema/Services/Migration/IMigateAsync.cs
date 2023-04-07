using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Services.Migration
{
    internal interface IMigrateAsync
    {
        Task MigrateAsync();
    }

    internal interface IMigrateSync
    {
        void Migrate();
    }
}
