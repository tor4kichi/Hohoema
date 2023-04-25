#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Contracts.Navigations;

public interface INavigationParameters : IDictionary<string, object>
{
    bool TryGetValue<T>(string key, out T outValue);
    T GetValue<T>(string key);
}

