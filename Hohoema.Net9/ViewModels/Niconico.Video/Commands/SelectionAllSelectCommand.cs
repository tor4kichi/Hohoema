﻿#nullable enable
namespace Hohoema.ViewModels.Niconico.Video.Commands;

public sealed partial class SelectionAllSelectCommand : CommandBase
{
    protected override bool CanExecute(object parameter)
    {
        return true;
    }

    protected override void Execute(object parameter)
    {
        var selectionContext = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<VideoItemsSelectionContext>();
        selectionContext.ToggleSelectAll();
    }
}
