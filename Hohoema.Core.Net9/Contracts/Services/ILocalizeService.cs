#nullable enable
using System.Collections.Generic;

namespace Hohoema.Contracts.Services;

public interface ILocalizeService
{
    string Translate(string key);
    string Translate(string key, params object[] args);

    string GetDefaultLocale();
    IReadOnlyList<string> GetAllLocales();
}
