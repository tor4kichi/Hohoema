namespace NiconicoToolkit.SnapshotSearch.JsonFilters
{
    public interface IJsonSearchFilter : ISearchFilter
	{
		IJsonSearchFilterData GetJsonFilterData();
    }
}
