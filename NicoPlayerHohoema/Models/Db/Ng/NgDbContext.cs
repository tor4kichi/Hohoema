using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models.Db
{
	public sealed class NgDbContext : DbContext
	{
		public DbSet<NgVideoOwnerUser> NgVideoOwnerUser { get; set; }
		public DbSet<NgVideoTitleKeyword> NgVideoTitleKeyword { get; set; }
		public DbSet<NgVideoTag> NgVideoTag { get; set; }

		public DbSet<NgCommentUserId> NgCommentUserId { get; set; }
		public DbSet<NgCommentKeyword> NgCommentKeyword { get; set; }


		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			var connectionString = new SqliteConnectionStringBuilder { DataSource = "Ng.db" }.ToString();
			optionsBuilder.UseSqlite(new SqliteConnection(connectionString));
		}

		public static Task InitializeAsync()
		{
			using (var db = new HistoryDbContext())
			{
				return db.Database.MigrateAsync();
			}
		}
	}


	public class NgVideoOwnerUser
	{
		[Key]
		public uint UserId { get; set; }


	}

	public class NgVideoTitleKeyword
	{
		[Key]
		public string Keyword { get; set; }
	}

	public class NgVideoTag
	{
		[Key]
		public string Tag { get; set; }

		// Lockが有効な時だけタグのNGを適用する
		public bool IgNgOnlyLock { get; set; }
	}

	public class NgCommentUserId
	{
		[Key]
		public uint UserId { get; set; }

		public bool IsAnnominity { get; set; }
	}

	public class NgCommentKeyword
	{
		[Key]
		public string Keyword { get; set; }
	}
}
