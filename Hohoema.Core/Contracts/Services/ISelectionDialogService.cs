using CommunityToolkit.Mvvm.ComponentModel;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hohoema.Contracts.Services;

public class SelectDialogPayload
{
    public string Label { get; set; }
    public string Id { get; set; }
    public object Context { get; set; }
}

public struct ContentSelectDialogDefaultSet
{
    public string DialogTitle { get; set; }
    public string ChoiceListTitle { get; set; }
    public List<SelectDialogPayload> ChoiceList { get; set; }

    public string TextInputTitle { get; set; }
    public Func<string, Task<List<SelectDialogPayload>>> GenerateCandiateList { get; set; }
}

public interface ISelectableContainer : INotifyPropertyChanged, IDisposable
{
    string Label { get; }
    SelectDialogPayload GetResult();
    bool IsValidatedSelection { get; }
    event Action<ISelectableContainer> SelectionItemChanged;
}

public interface ISelectionDialogService
{
    Task<SelectDialogPayload> ShowContentSelectDialogAsync(ContentSelectDialogDefaultSet dialogContentSet);
    Task<SelectDialogPayload> ShowContentSelectDialogAsync(string dialogTitle, IEnumerable<ISelectableContainer> containers, int defaultContainerIndex = 0);
    Task<List<T>> ShowMultiChoiceDialogAsync<T, X>(string title, IEnumerable<T> selectableItems, IEnumerable<T> selectedItems, Expression<Func<T, X>> memberPathExpression);
    Task<List<T>> ShowMultiChoiceDialogAsync<T>(string title, IEnumerable<T> selectableItems, IEnumerable<T> selectedItems, string memberPathName);
    Task<List<T>> ShowMultiSelectDialogAsync<T>(List<T> sourceItems, string displayMemberPath, Func<T, string, bool> filter, string dialogTitle, string dialogPrimaryButtonText);
    Task<T> ShowSingleSelectDialogAsync<T>(List<T> sourceItems, string displayMemberPath, Func<T, string, bool> filter, string dialogTitle, string dialogPrimaryButtonText, string dialogSecondaryButtonText = null, Func<Task<T>> SecondaryButtonAction = null);
}