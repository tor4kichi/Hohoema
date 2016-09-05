using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinRTXamlToolkit.Async;

namespace NicoPlayerHohoema.Models.Db
{
	public static class UserInfoDb
	{
		readonly static AsyncLock _AsyncLock = new AsyncLock();

		public static async Task AddOrReplaceAsync(string userId, string name, string iconUrl = null)
		{
			bool isAlreadHasUser = await GetAsync(userId) != null;

			using (var releaser = await _AsyncLock.LockAsync())
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

					db.Users.Update(info);
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

		public static async Task<UserInfo> GetAsync(string userId)
		{
			using (var releaser = await _AsyncLock.LockAsync())
			using (var db = new NicoVideoDbContext())
			{
				return await db.Users.SingleOrDefaultAsync(x => x.UserId == userId);
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
