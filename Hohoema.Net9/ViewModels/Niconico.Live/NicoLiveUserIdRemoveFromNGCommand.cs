﻿#nullable enable
using Hohoema.Services.Player.Videos;

namespace Hohoema.ViewModels.Niconico.Live;

public sealed partial class NicoLiveUserIdRemoveFromNGCommand : CommandBase
{
    private readonly CommentFilteringFacade _playerSettings;

    public NicoLiveUserIdRemoveFromNGCommand(CommentFilteringFacade playerSettings)
    {
        _playerSettings = playerSettings;
    }

    protected override bool CanExecute(object parameter)
    {
        return parameter is string;
    }

    protected override void Execute(object parameter)
    {
        _playerSettings.RemoveFilteringCommentOwnerId(parameter as string);
    }
}
