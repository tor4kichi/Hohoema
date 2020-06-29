using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Hohoema.UseCase.Services
{
    public interface IMultiSelectionDialogService
    {
        Task<List<T>> ShowMultiChoiceDialogAsync<T, X>(string title, IEnumerable<T> selectableItems, IEnumerable<T> selectedItems, Expression<Func<T, X>> memberPathExpression);
        Task<List<T>> ShowMultiChoiceDialogAsync<T>(string title, IEnumerable<T> selectableItems, IEnumerable<T> selectedItems, string memberPathName);
    }
}