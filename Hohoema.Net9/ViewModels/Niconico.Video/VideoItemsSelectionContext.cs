﻿#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using Hohoema.Models.Niconico.Video;
using System;
using System.Collections.ObjectModel;

namespace Hohoema.ViewModels.Niconico.Video;

public sealed class RequestSelectionStartEventArgs
{
    public IVideoContent FirstSelectedItem { get; internal set; }
}


public sealed class RequestSelectAllEventArgs
{
    
}


public sealed class VideoItemsSelectionContext : ObservableObject
{
    private bool _isSelectionEnabled;
    public bool IsSelectionEnabled
    {
        get { return _isSelectionEnabled; }
        private set { SetProperty(ref _isSelectionEnabled, value); }
    }

    public ObservableCollection<IVideoContent> SelectionItems { get; }

    public event EventHandler<RequestSelectionStartEventArgs> SelectionStarted;
    public event EventHandler<RequestSelectAllEventArgs> RequestSelectAll;

    public VideoItemsSelectionContext()
    {
        SelectionItems = new ObservableCollection<IVideoContent>();
    }


    public void StartSelection(IVideoContent firstSelectedItem = null)
    {
        if (!IsSelectionEnabled)
        {
            IsSelectionEnabled = true;

            if (firstSelectedItem != null)
            {
                SelectionItems.Add(firstSelectedItem);
                SelectionStarted?.Invoke(this, new RequestSelectionStartEventArgs()
                {
                    FirstSelectedItem = firstSelectedItem
                });
            }
        }
    }

    public void EndSelectioin()
    {
        IsSelectionEnabled = false;
        SelectionItems.Clear();
    }

    public void ToggleSelectAll()
    {
        RequestSelectAll?.Invoke(this, new RequestSelectAllEventArgs());
    }
}
