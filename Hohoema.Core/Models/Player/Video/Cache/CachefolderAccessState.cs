#nullable enable
namespace Hohoema.Models.Player.Video.Cache;

public enum CacheFolderAccessState
{
    NotAccepted,
    NotEnabled,
    NotSelected,
    SelectedButNotExist,
    Exist
}
