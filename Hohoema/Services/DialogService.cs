using I18NPortable;
using Hohoema.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Hohoema.Views.Dialogs;
using Hohoema.Models.Application;
using Hohoema.Services.LocalMylist;
using Hohoema.Services.Niconico;
using Hohoema.Contracts.Services;
using Reactive.Bindings;
using System.Collections.ObjectModel;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Threading;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;

namespace Hohoema.Services;


public abstract class SelectableContainerBase : ObservableObject, ISelectableContainer
{
    private bool _IsSelected;
    public bool IsSelected
    {
        get { return _IsSelected; }
        set { SetProperty(ref _IsSelected, value); }
    }

    private string _Label;
    public string Label
    {
        get { return _Label; }
        set { SetProperty(ref _Label, value); }
    }

    public SelectableContainerBase(string label)
    {
        Label = label;
    }



    public abstract SelectDialogPayload GetResult();

    public virtual void Dispose()
    {

    }

    public abstract bool IsValidatedSelection { get; }
    public abstract event Action<ISelectableContainer> SelectionItemChanged;


}

public class TextInputSelectableContainer : SelectableContainerBase
{
    private SynchronizationContextScheduler _CurrentWindowContextScheduler;
    public SynchronizationContextScheduler CurrentWindowContextScheduler
    {
        get
        {
            return _CurrentWindowContextScheduler
                ?? (_CurrentWindowContextScheduler = new SynchronizationContextScheduler(SynchronizationContext.Current));
        }
    }

    public Func<string, Task<List<SelectDialogPayload>>> GenerateCandidateList { get; private set; }
    Helpers.AsyncLock _UpdateCandidateListLock = new Helpers.AsyncLock();

    CompositeDisposable _CompositeDisposable;

    public TextInputSelectableContainer(string label, Func<string, Task<List<SelectDialogPayload>>> generateCandiateList, string defaultText = "")
        : base(label)
    {
        _CompositeDisposable = new CompositeDisposable();
        IsSelectFromCandidate = generateCandiateList != null;
        GenerateCandidateList = generateCandiateList;
        NowUpdateCandidateList = new ReactiveProperty<bool>(CurrentWindowContextScheduler, false)
            .AddTo(_CompositeDisposable);

        Text = new ReactiveProperty<string>(CurrentWindowContextScheduler, defaultText)
            .AddTo(_CompositeDisposable);
        CandidateItems = new ObservableCollection<SelectDialogPayload>();
        SelectedItem = new ReactiveProperty<SelectDialogPayload>(CurrentWindowContextScheduler)
            .AddTo(_CompositeDisposable);

        if (IsSelectFromCandidate)
        {
            var dispatcher = Windows.UI.Xaml.Window.Current.Dispatcher;
#pragma warning disable IDISP004 // Don't ignore created IDisposable.
            Text
                .Throttle(TimeSpan.FromSeconds(0.5))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Subscribe(async x =>
                {
                    using (var releaser = await _UpdateCandidateListLock.LockAsync())
                    {
                        NowUpdateCandidateList.Value = true;

                        var list = await GenerateCandidateList.Invoke(x);

                        // 表示候補を取得
                        await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            SelectedItem.Value = null;

                            CandidateItems.Clear();

                            if (list == null) { return; }

                            foreach (var item in list)
                            {
                                CandidateItems.Add(item);
                            }

                            // 候補が一つだけの場合は予め選択
                            if (CandidateItems.Count == 1)
                            {
                                SelectedItem.Value = CandidateItems.First();
                            }
                        });

                        NowUpdateCandidateList.Value = false;
                    }
                })
            .AddTo(_CompositeDisposable);
#pragma warning restore IDISP004 // Don't ignore created IDisposable.

#pragma warning disable IDISP004 // Don't ignore created IDisposable.
            SelectedItem.Subscribe(x =>
            {
                _IsValidatedSelection = x != null;
                OnPropertyChanged(nameof(IsValidatedSelection));

                SelectionItemChanged?.Invoke(this);
            })
            .AddTo(_CompositeDisposable);
#pragma warning restore IDISP004 // Don't ignore created IDisposable.
        }
        else
        {
#pragma warning disable IDISP004 // Don't ignore created IDisposable.
            Text.Subscribe(x =>
            {
                _IsValidatedSelection = !string.IsNullOrEmpty(x);
                OnPropertyChanged(nameof(IsValidatedSelection));

                SelectionItemChanged?.Invoke(this);
            })
            .AddTo(_CompositeDisposable);
#pragma warning restore IDISP004 // Don't ignore created IDisposable.
        }
    }


    public override void Dispose()
    {
        _CompositeDisposable.Dispose();
        Text?.Dispose();
        SelectedItem?.Dispose();
        NowUpdateCandidateList?.Dispose();
        base.Dispose();
    }

    private bool _IsValidatedSelection;
    public override bool IsValidatedSelection => _IsValidatedSelection;

    public ReactiveProperty<string> Text { get; private set; }


    public bool IsSelectFromCandidate { get; private set; }
    public ObservableCollection<SelectDialogPayload> CandidateItems { get; private set; }
    public ReactiveProperty<SelectDialogPayload> SelectedItem { get; private set; }
    public ReactiveProperty<bool> NowUpdateCandidateList { get; private set; }


    public override event Action<ISelectableContainer> SelectionItemChanged;

    public override SelectDialogPayload GetResult()
    {
        if (IsSelectFromCandidate)
        {
            return SelectedItem.Value;
        }
        else
        {
            return new SelectDialogPayload()
            {
                Label = Text.Value,
                Id = Text.Value
            };
        }
    }
}

public class ChoiceFromListSelectableContainer : SelectableContainerBase
{
    public ChoiceFromListSelectableContainer(string label, IEnumerable<SelectDialogPayload> selectableItems)
        : base(label)
    {
        Items = selectableItems.ToList();
        SelectedItem = Items.FirstOrDefault();
    }


    public List<SelectDialogPayload> Items { get; private set; }



    private bool _IsValidatedSelection;
    public override bool IsValidatedSelection => _IsValidatedSelection;

    private SelectDialogPayload _SelectedItem;
    public SelectDialogPayload SelectedItem
    {
        get { return _SelectedItem; }
        set
        {
            if (SetProperty(ref _SelectedItem, value))
            {
                _IsValidatedSelection = _SelectedItem != null;
                OnPropertyChanged(nameof(IsValidatedSelection));

                SelectionItemChanged?.Invoke(this);
            }
        }
    }

    public override event Action<ISelectableContainer> SelectionItemChanged;

    public override SelectDialogPayload GetResult()
    {
        return SelectedItem;
    }
}

public class DialogService : IDialogService, IMylistGroupDialogService, ISelectionDialogService
{
    private readonly CurrentActiveWindowUIContextService _currentActiveWindowUIContextService;

    public LoginUserOwnedMylistManager UserMylistManager { get; }
    public LocalMylistManager _localMylistManager { get; }
    public DialogService(
        CurrentActiveWindowUIContextService currentActiveWindowUIContextService
        )
    {
        _currentActiveWindowUIContextService = currentActiveWindowUIContextService;
    }

    #region Update Notice Dialog

    public async Task ShowLatestUpdateNotice()
    {
        var text = await AppUpdateNotice.GetUpdateNoticeAsync();
        var dialog = new Dialogs.MarkdownTextDialog("UpdateNotice".Translate());
        dialog.Text = text;
        dialog.PrimaryButtonText = "Close".Translate();

        await dialog.ShowAsync();
    }


    #endregion


    #region ContentSelectDialog

    public Task<SelectDialogPayload> ShowContentSelectDialogAsync(ContentSelectDialogDefaultSet dialogContentSet)
    {
        var choiceListContainer = new ChoiceFromListSelectableContainer(dialogContentSet.ChoiceListTitle, dialogContentSet.ChoiceList);
        var customTextContainer = new TextInputSelectableContainer(dialogContentSet.TextInputTitle, dialogContentSet.GenerateCandiateList);

        var containers = new List<ISelectableContainer>();

        ISelectableContainer firstSelected = null;
        if (!string.IsNullOrEmpty(dialogContentSet.ChoiceListTitle))
        {
            containers.Add(choiceListContainer);

            if (choiceListContainer.Items.Count > 0)
            {
                firstSelected = choiceListContainer;
            }
            else
            {
                firstSelected = choiceListContainer;
            }
        }

        if (!string.IsNullOrEmpty(dialogContentSet.TextInputTitle))
        {
            containers.Add(customTextContainer);

            if (firstSelected == null)
            {
                firstSelected = customTextContainer;
            }
        }

        return ShowContentSelectDialogAsync(dialogContentSet.DialogTitle, containers, firstSelected);


    }

    public Task<SelectDialogPayload> ShowContentSelectDialogAsync(string dialogTitle, IEnumerable<ISelectableContainer> containers, int defaultContainerIndex = 0)
    {
        var firstSelected = containers.ElementAtOrDefault(defaultContainerIndex);
        return ShowContentSelectDialogAsync(dialogTitle, containers, firstSelected);
    }

    private async Task<SelectDialogPayload> ShowContentSelectDialogAsync(string dialogTitle, IEnumerable<ISelectableContainer> containers, ISelectableContainer firstSelected)
    {
        var context = new ContentSelectDialogContext(dialogTitle, containers, firstSelected);

        SelectDialogPayload resultContent = null;
        try
        {
            var dialog = new Dialogs.ContentSelectDialog()
            {
                DataContext = context
            };

            var dialogResult = await dialog.ShowAsync();
            if (dialogResult == Windows.UI.Xaml.Controls.ContentDialogResult.Primary)
            {
                resultContent = context.GetResult();
            }
        }
        finally
        {
            context?.Dispose();
        }

        return resultContent;
    }

    #endregion



    #region RankingChoiceDialog

    public Task<List<T>> ShowMultiChoiceDialogAsync<T, X>(
        string title,
        IEnumerable<T> selectableItems,
        IEnumerable<T> selectedItems,
        Expression<Func<T, X>> memberPathExpression
        )
    {
        return ShowMultiChoiceDialogAsync(
            title,
            selectableItems,
            selectedItems,
            ((MemberExpression)memberPathExpression.Body).Member.Name
            );
    }


    public async Task<List<T>> ShowMultiChoiceDialogAsync<T>(
        string title,
        IEnumerable<T> selectableItems,
        IEnumerable<T> selectedItems,
        string memberPathName
        )
    {
        var multiChoiceDialog = new Dialogs.MultiChoiceDialog();

        multiChoiceDialog.Title = title;
        multiChoiceDialog.Items = selectableItems.ToList();
        multiChoiceDialog.SelectedItems = selectedItems.ToList();
        multiChoiceDialog.DisplayMemberPath = memberPathName;

        var result = await multiChoiceDialog.ShowAsync();

        if (result == Windows.UI.Xaml.Controls.ContentDialogResult.Primary)
        {
            return multiChoiceDialog.SelectedItems.Cast<T>().ToList();
        }
        else
        {
            return null;
        }
    }


    #endregion



    #region EditMylistGroupDialog


    public Task<bool> ShowEditMylistGroupDialogAsync(MylistGroupEditData data)
    {
        return ShowMylistGroupDialogAsync(data, false);

    }
    public Task<bool> ShowCreateMylistGroupDialogAsync(MylistGroupEditData data)
    {
        return ShowMylistGroupDialogAsync(data, true);
    }

    private async Task<bool> ShowMylistGroupDialogAsync(MylistGroupEditData data, bool isCreate)
    {
        var context = new EditMylistGroupDialogContext(data, isCreate);
        var dialog = new EditMylistGroupDialog()
        {
            DataContext = context
        };

        var result = await dialog.ShowAsync();

        if (result == Windows.UI.Xaml.Controls.ContentDialogResult.Primary)
        {
            var resultData = context.GetResult();
            data.Name = resultData.Name;
            data.Description = resultData.Description;
            data.IsPublic = resultData.IsPublic;
            data.DefaultSortKey = resultData.DefaultSortKey;
            data.DefaultSortOrder = resultData.DefaultSortOrder;
        }
        return result == Windows.UI.Xaml.Controls.ContentDialogResult.Primary;
    }


    #endregion


    #region GetTextDialog

    public async Task<string> GetTextAsync(string title, string placeholder, string defaultText = "", Func<string, bool> validater = null)
    {
        if (validater == null)
        {
            validater = (_) => true;
        }
        var context = new TextInputDialogContext(title, placeholder, defaultText, validater);

        var dialog = new TextInputDialog()
        {
            DataContext = context
        };

        var result = await dialog.ShowAsync();

        // 仮想入力キーボードを閉じる
        Windows.UI.ViewManagement.InputPane.GetForCurrentView().TryHide();

        if (result == Windows.UI.Xaml.Controls.ContentDialogResult.Primary)
        {
            return context.GetValidText();
        }
        else
        {
            return null;
        }
    }

    #endregion


    #region Niconico Two Factor Auth Dialog


    public async Task ShowNiconicoTwoFactorLoginDialog(object content)
    {
        var dialog = new NiconicoTwoFactorAuthDialog();

        dialog.WebViewContent = content;
        await dialog.ShowAsync();
    }

    #endregion




    public async Task<bool> ShowMessageDialog(string content, string title, string acceptButtonText = null, string cancelButtonText = null)
    {
        var dialog = new MessageDialog(content, title);
        if (acceptButtonText != null)
        {
            dialog.Commands.Add(new UICommand(acceptButtonText) { Id = "accept" });
        }

        if (cancelButtonText != null)
        {
            dialog.Commands.Add(new UICommand(cancelButtonText) { Id = "cancel" });
        }

        var result = await dialog.ShowAsync();

        return (result?.Id as string) == "accept";
    }


    #region AdvancedSelectDialog

    public async Task<T> ShowSingleSelectDialogAsync<T>(
        List<T> sourceItems,
        string displayMemberPath,
        Func<T, string, bool> filter,
        string dialogTitle,
        string dialogPrimaryButtonText,
        string dialogSecondaryButtonText = null,
        Func<Task<T>> SecondaryButtonAction = null
        )
    {
        var advancedSelectDialog = new AdvancedSelectDialog()
        {
            XamlRoot = _currentActiveWindowUIContextService.XamlRoot,
        };
        advancedSelectDialog.Title = dialogTitle;
        advancedSelectDialog.PrimaryButtonText = dialogPrimaryButtonText;
        advancedSelectDialog.CloseButtonText = "Cancel".Translate();
        advancedSelectDialog.SetSourceItems(sourceItems, filter != null ? (x, s) => filter((T)x, s) : default(Func<object, string, bool>));
        advancedSelectDialog.ItemDisplayMemberPath = displayMemberPath;
        advancedSelectDialog.IsMultiSelection = false;
        if (dialogSecondaryButtonText != null)
        {
            advancedSelectDialog.SecondaryButtonText = dialogSecondaryButtonText;
        }

        var result = await advancedSelectDialog.ShowAsync();
        if (result == Windows.UI.Xaml.Controls.ContentDialogResult.Primary)
        {
            return (T)advancedSelectDialog.GetResultItems().FirstOrDefault();
        }
        else if (result == Windows.UI.Xaml.Controls.ContentDialogResult.Secondary)
        {
            return await SecondaryButtonAction?.Invoke();
        }
        else
        {
            return default(T);
        }
    }

    public async Task<List<T>> ShowMultiSelectDialogAsync<T>(
        List<T> sourceItems,
        string displayMemberPath,
        Func<T, string, bool> filter,
        string dialogTitle,
        string dialogPrimaryButtonText
        )
    {
        var advancedSelectDialog = new AdvancedSelectDialog()
        {
            XamlRoot = _currentActiveWindowUIContextService.XamlRoot,
        };
        advancedSelectDialog.Title = dialogTitle;
        advancedSelectDialog.PrimaryButtonText = dialogPrimaryButtonText;
        advancedSelectDialog.SecondaryButtonText = "Cancel".Translate();
        advancedSelectDialog.SetSourceItems(sourceItems, filter != null ? (x, s) => filter((T)x, s) : default(Func<object, string, bool>));
        advancedSelectDialog.ItemDisplayMemberPath = displayMemberPath;
        advancedSelectDialog.IsMultiSelection = true;
        var result = await advancedSelectDialog.ShowAsync();
        if (result == Windows.UI.Xaml.Controls.ContentDialogResult.Primary)
        {
            return advancedSelectDialog.GetResultItems().Cast<T>().ToList();
        }
        else
        {
            return new List<T>();
        }
    }



    #endregion

}
