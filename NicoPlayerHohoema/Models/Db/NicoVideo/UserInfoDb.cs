using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models.Db
{
	public static class UserInfoDb
	{
		public static void AddOrReplace(string userId, string name, string iconUrl = null)
		{
			bool isAlreadHasUser = Get(userId) != null;

			using (var db = new NicoVideoDbContext())
			{
				var info = new UserInfo()
				{
					UserId = userId,
					Name = name,
					IconUri = iconUrl
				};
				if (isAlreadHasUser)
				{
					db.Users.Update(info);
				}
				else
				{
					db.Users.Add(info);
				}
				db.SaveChanges();
			}
		}

		public static async Task AddOrReplaceAsync(string userId, string name, string iconUrl = null)
		{
			bool isAlreadHasUser = await GetAsync(userId) != null;

			using (var db = new NicoVideoDbContext())
			{
				if (isAlreadHasUser)
				{
					var info = await db.Users.SingleAsync(x => x.UserId == userId);
					if (name != null)
					{
						info.Name = name;
					}
					if (iconUrl != null)
					{
						info.IconUri = iconUrl;
					}
				}
				else
				{
					var info = new UserInfo()
					{
						UserId = userId,
						Name = name,
						IconUri = iconUrl
					};

					db.Users.Add(info);
				}

				await db.SaveChangesAsync();
			}
		}

		public static UserInfo Get(string userId)
		{
			using (var db = new NicoVideoDbContext())
			{
				return db.Users.SingleOrDefault(x => x.UserId == userId);
			}
		}

		public static Task<UserInfo> GetAsync(string userId)
		{
			using (var db = new NicoVideoDbContext())
			{
				return db.Users.SingleOrDefaultAsync(x => x.UserId == userId);
			}
		}



		public static void Remove(UserInfo info)
		{
			using (var db = new NicoVideoDbContext())
			{
				db.Users.Remove(info);
				db.SaveChanges();
			}
		}
	}

}
