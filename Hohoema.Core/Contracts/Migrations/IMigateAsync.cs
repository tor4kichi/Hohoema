#nullable enable
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
