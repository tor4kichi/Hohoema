using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models.Db
{
	public static class SearchHistoryDb
	{

		public static int GetHistoryCount()
		{
			using (var db = new HistoryDbContext())
			{
				return db.SearchHistory.Count();
			}
		}

		public const uint MaxSearchHistoryCount = 30;

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
						LastUpdated = DateTime.Now
					};

					db.SearchHistory.Add(searchHistory);


					if ( db.SearchHistory.Count() > MaxSearchHistoryCount)
					{
						// 一番古いアイテムを削除
						var recentItem = db.SearchHistory.OrderBy(x => x.LastUpdated).First();
						db.SearchHistory.Remove(recentItem);
					}
				}

				else
				{
					searchHistory.SearchCount++;
					searchHistory.LastUpdated = DateTime.Now;

					db.SearchHistory.Update(searchHistory);
				}

				db.SaveChanges();
			}
		}

		public static List<SearchHistory> GetHistoryItems()
		{
			using (var db = new HistoryDbContext())
			{
				return db.SearchHistory.OrderByDescending(x => x.LastUpdated).ToList();
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


		public static void Clear()
		{
			using (var db = new HistoryDbContext())
			{
				var items = db.SearchHistory.ToList();
				db.SearchHistory.RemoveRange(items);
				db.SaveChanges();
			}
		}
	}
}
