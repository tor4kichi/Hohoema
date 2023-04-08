using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Contracts.Migrations;

public interface IMigrateAsync
{
    Task MigrateAsync();
}

public interface IMigrateSync
{
    void Migrate();
}
