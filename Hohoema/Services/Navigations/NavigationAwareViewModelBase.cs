using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Hohoema.Services.Navigations
{
    public interface INavigationResult
    {
        bool IsSuccess { get; }
        Exception Exception { get; }
    }

    public class NavigationResult : INavigationResult
    {
        public bool IsSuccess { get; init; }

        public Exception Exception { get; init; }
    }

    public interface INavigationParameters : IDictionary<string, object>
    {
        bool TryGetValue<T>(string key, out T outValue);
        T GetValue<T>(string key);
    }

    public class NavigationParameters : Dictionary<string, object>, INavigationParameters
    {
        static IEnumerable<KeyValuePair<string, object>> ParseQueryParametersString(string queryParameters)
        {
            var query = HttpUtility.ParseQueryString(queryParameters);
            return query.AllKeys.Select(x => new KeyValuePair<string, object>(x, query.Get(x)));
        }
        public NavigationParameters(string queryParameters)
            : base(ParseQueryParametersString(queryParameters))
        {
            
        }

        public NavigationParameters(IEnumerable<KeyValuePair<string, object>> parameters)
            : base(parameters)
        {
        }

        public NavigationParameters(params (string Key, object Value)[] parameters)
            : base(parameters.Select(x => new KeyValuePair<string, object>(x.Key, x.Value)))
        {
        }

        public bool TryGetValue<T>(string key, out T outValue)
        {
            if (base.TryGetValue(key, out object temp))
            {
                var type = typeof(T);
                if (type.IsEnum && temp is string strTemp)
                {
                    outValue = (T)Enum.Parse(type, strTemp);
                    return true;
                }
                else if (temp.GetType() == typeof(T))
                {                    
                    outValue = (T)temp;
                    return true;
                }
                else
                {
                    outValue = default;
                    return false;
                }
            }
            else
            {
                outValue = default(T);
                return false;
            }
        }

        public T GetValue<T>(string key)
        {
            return TryGetValue(key, out T value) ? value : throw new KeyNotFoundException();
        }
    }

    public interface INavigationAware
    {
        void OnNavigatedFrom(INavigationParameters parameters);
        void OnNavigatingTo(INavigationParameters parameters);
        void OnNavigatedTo(INavigationParameters parameters);
        Task OnNavigatedToAsync(INavigationParameters parameters);
    }

    public abstract class NavigationAwareViewModelBase : ObservableObject, INavigationAware
    {
        public virtual void OnNavigatedFrom(INavigationParameters parameters)
        {

        }

        public virtual void OnNavigatingTo(INavigationParameters parameters)
        {

        }

        public virtual void OnNavigatedTo(INavigationParameters parameters)
        {

        }

        public virtual Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            return Task.CompletedTask;
        }
    }
}
