using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models.Db.History
{
	public static class SearchHistoryDb
	{
		public static void Searched(string keyword, SearchTarget target)
		{
			using (var db = new HistoryDbContext())
			{
				var searchHistory = db.SearchHistory.SingleOrDefault(x => x.Keyword == keyword && x.Target == target);

				if (searchHistory == null)
				{
					searchHistory = new SearchHistory()
					{
						Keyword = keyword,
						Target = target,
						SearchCount = 1,
					};

					db.SearchHistory.Add(searchHistory);
				}
				else
				{
					searchHistory.SearchCount++;
				}

				db.SaveChanges();
			}
		}

		public static bool RemoveHistory(string keyword, SearchTarget target)
		{
			bool removeSuccess = false;

			using (var db = new HistoryDbContext())
			{
				var searchHistory = db.SearchHistory.SingleOrDefault(x => x.Keyword == keyword && x.Target == target);

				if (searchHistory != null)
				{
					db.SearchHistory.Remove(searchHistory);
					removeSuccess = true;
				}

				db.SaveChanges();
			}

			return removeSuccess;
		}
	}
}
