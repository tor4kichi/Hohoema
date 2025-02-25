#nullable enable
using Hohoema.Services.LocalMylist;

namespace Hohoema.ViewModels.Hohoema.LocalMylist;

public sealed partial class LocalMylistCreateCommand : CommandBase
{
    private readonly LocalMylistManager _localMylistManager;

    public LocalMylistCreateCommand(LocalMylistManager localMylistManager)
    {
        _localMylistManager = localMylistManager;
    }

    protected override bool CanExecute(object parameter)
    {
        if (parameter is string p)
        {
            return !string.IsNullOrWhiteSpace(p);
        }
        else
        {
            return false;
        }
    }

    protected override void Execute(object parameter)
    {
        if (parameter is string label)
        {
            _localMylistManager.CreatePlaylist(label);
        }
    }
}
