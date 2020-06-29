using Hohoema.Dialogs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hohoema.Services
{
    internal interface ISingleSelectionDialogService
    {
        Task<SelectDialogPayload> ShowContentSelectDialogAsync(ContentSelectDialogDefaultSet dialogContentSet);
        Task<SelectDialogPayload> ShowContentSelectDialogAsync(string dialogTitle, IEnumerable<ISelectableContainer> containers, int defaultContainerIndex = 0);
    }
}