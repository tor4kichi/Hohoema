using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hohoema.Services;

public interface IDialogService
{
    Task<string> GetTextAsync(string title, string placeholder, string defaultText = "", Func<string, bool> validater = null);
    Task<bool> ShowMessageDialog(string content, string title, string acceptButtonText = null, string cancelButtonText = null);
    Task<T> ShowSingleSelectDialogAsync<T>(List<T> sourceItems, string displayMemberPath, Func<T, string, bool> filter, string dialogTitle, string dialogPrimaryButtonText, string dialogSecondaryButtonText = null, Func<Task<T>> SecondaryButtonAction = null);
}