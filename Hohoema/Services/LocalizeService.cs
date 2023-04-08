using I18NPortable;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Hohoema.Services;

internal class LocalizeService : ILocalizeService
{
    public IReadOnlyList<string> GetAllLocales()
    {
        return I18N.Current.Languages.Select(x => x.Locale).ToImmutableArray();
    }

    public string GetDefaultLocale()
    {
        return I18N.Current.GetDefaultLocale();
    }

    public string Translate(string key)
    {
        return key.Translate();
    }

    public string Translate(string key, params object[] args)
    {
        return key.Translate(args);
    }
}
