using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Contracts.Services;

public interface ILocalizeService
{
    string Translate(string key);
    string Translate(string key, params object[] args);

    string GetDefaultLocale();
    IReadOnlyList<string> GetAllLocales();
}
