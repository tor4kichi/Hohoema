using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models.Db
{
	static public class CacheRequestDb
	{

		public static List<CacheRequest> GetList()
		{
			using (var db = new NicoVideoDbContext())
			{
				return db.CacheRequests.ToList();
			}
		}

		public static void RequestCache(string threadId, NicoVideoQuality quality)
		{
			using (var db = new NicoVideoDbContext())
			{
				var req = db.CacheRequests.SingleOrDefault(x => x.ThreadId == threadId && x.Quality == quality);
				if (req == null)
				{
					db.CacheRequests.Add(new CacheRequest()
					{
						ThreadId = threadId,
						Quality = quality,
						RequestTime = DateTime.Now
					});
				}
				else
				{
					req.RequestTime = DateTime.Now;
				}

				db.SaveChanges();
			}
		}

		public static bool CheckCacheRequested(string threadId, NicoVideoQuality quality)
		{
			using (var db = new NicoVideoDbContext())
			{
				return db.CacheRequests.Any(x => x.ThreadId == threadId && x.Quality == quality);
			}
		}

		public static bool CancelRequest(string threadId, NicoVideoQuality quality)
		{
			using (var db = new NicoVideoDbContext())
			{
				var req = db.CacheRequests.SingleOrDefault(x => x.ThreadId == threadId && x.Quality == quality);
				if (req != null)
				{
					db.CacheRequests.Remove(req);
					db.SaveChanges();

					return true;
				}
				else
				{
					return false;
				}
			}
		}


		public static void Deleted(string threadId)
		{
			using (var db = new NicoVideoDbContext())
			{
				var req = db.CacheRequests.Where(x => x.ThreadId == threadId).ToList();
				if (req.Count > 0)
				{
					db.CacheRequests.RemoveRange(req);
					db.SaveChanges();
				}
			}
		}

		public static void Clear()
		{
			using (var db = new NicoVideoDbContext())
			{
				var list = db.CacheRequests.ToList();
				db.CacheRequests.RemoveRange(list);

				db.SaveChanges();
			}
		}
	}
}
