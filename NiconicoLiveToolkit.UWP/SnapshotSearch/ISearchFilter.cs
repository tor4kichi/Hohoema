using System.Collections.Generic;

namespace NiconicoToolkit.SnapshotSearch
{
    public interface ISearchFilter
    {
		IEnumerable<KeyValuePair<string, string>> GetFilterKeyValues();
    }

}
