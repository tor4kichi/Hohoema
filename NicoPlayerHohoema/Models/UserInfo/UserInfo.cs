using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models.UserInfo
{
	public sealed class UserInfoDbContext : DbContext
	{
		public DbSet<UserInfo> UserInfos { get; set; }

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			var connectionString = new SqliteConnectionStringBuilder { DataSource = "UserInfo.db" }.ToString();
			optionsBuilder.UseSqlite(new SqliteConnection(connectionString));
		}


		public static Task InitializeAsync()
		{
			using (var db = new UserInfoDbContext())
			{
				return db.Database.EnsureCreatedAsync();
			}
		}

		public static void AddOrReplace(string userId, string name, string iconUrl = null)
		{
			bool isAlreadHasUser = Get(userId) != null;

			using (var db = new UserInfoDbContext())
			{
				var info = new UserInfo()
				{
					UserId = userId,
					Name = name,
					IconUri = iconUrl
				};
				if (isAlreadHasUser)
				{
					db.UserInfos.Update(info);
				}
				else
				{
					db.UserInfos.Add(info);
				}
				db.SaveChanges();
			}
		}

		public static async Task AddOrReplaceAsync(string userId, string name, string iconUrl = null)
		{
			bool isAlreadHasUser = await GetAsync(userId) != null;

			using (var db = new UserInfoDbContext())
			{
				if (isAlreadHasUser)
				{
					var info = await db.UserInfos.SingleAsync(x => x.UserId == userId);
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

					db.UserInfos.Add(info);
				}

				await db.SaveChangesAsync();
			}
		}

		public static UserInfo Get(string userId)
		{
			using (var db = new UserInfoDbContext())
			{
				return db.UserInfos.SingleOrDefault(x => x.UserId == userId);
			}
		}

		public static Task<UserInfo> GetAsync(string userId)
		{
			using (var db = new UserInfoDbContext())
			{
				return db.UserInfos.SingleOrDefaultAsync(x => x.UserId == userId);
			}
		}

		

		public static void Remove(UserInfo info)
		{
			using (var db = new UserInfoDbContext())
			{
				db.UserInfos.Remove(info);
				db.SaveChanges();
			}
		}
	}

	public class UserInfo
	{
		[Key]
		public string UserId { get; set; }

		public string Name { get; set; }

		public string IconUri { get; set; }
	}


	public class UserInfoComperer : IEqualityComparer<UserInfo>
	{
		public static readonly UserInfoComperer Default = new UserInfoComperer();

		public bool Equals(UserInfo x, UserInfo y)
		{
			return x.UserId == y.UserId;
		}

		public int GetHashCode(UserInfo obj)
		{
			return obj.UserId.GetHashCode();
		}
	}
}
