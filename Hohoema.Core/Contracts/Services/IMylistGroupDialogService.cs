using NiconicoToolkit.Mylist;
using System.Threading.Tasks;

namespace Hohoema.Contracts.Services;

public class MylistGroupEditData
{
    public string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
    public MylistSortKey DefaultSortKey { get; set; }
    public MylistSortOrder DefaultSortOrder { get; set; }

    public MylistGroupEditData()
    {

    }
}

public interface IMylistGroupDialogService : ISelectionDialogService
{
    Task<bool> ShowCreateMylistGroupDialogAsync(MylistGroupEditData data);
    Task<bool> ShowEditMylistGroupDialogAsync(MylistGroupEditData data);
}