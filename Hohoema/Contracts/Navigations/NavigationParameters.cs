#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using Hohoema.Contracts.Navigations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Windows.UI.Xaml.Navigation;

namespace Hohoema.Contracts.Navigations;

public class NavigationResult : INavigationResult
{
    public bool IsSuccess { get; init; }

    public Exception Exception { get; init; }
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


public static class NavigationParametersExtensions
{
    public static bool TryGetValue<T>(this INavigationParameters parameters, string key, out T outValue)
    {
        if (!parameters.ContainsKey(key))
        {
            outValue = default(T);
            return false;
        }
        else
        {
            return parameters.TryGetValue(key, out outValue);
        }
    }

    public const string NavigationModeKey = "__nm";
    public static NavigationMode GetNavigationMode(this INavigationParameters parameters)
    {
        return parameters.TryGetValue<NavigationMode>(NavigationModeKey, out var mode) ? mode : throw new InvalidOperationException();
    }

    public static void SetNavigationMode(this INavigationParameters parameters, NavigationMode mode)
    {
        if (parameters == null) { return; }

        parameters.Remove(NavigationModeKey);
        parameters.Add(NavigationModeKey, mode);
    }
}
