using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Data.Sqlite;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NicoPlayerHohoema.Models.Db
{
	public sealed class HistoryDbContext : DbContext
	{
		public DbSet<VideoPlayHistory> VideoPlayHistory { get; set; }
		public DbSet<SearchHistory> SearchHistory { get; set; }

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			var connectionString = new SqliteConnectionStringBuilder { DataSource = "History.db" }.ToString();
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


	public class VideoPlayHistory
	{
		[Key]
		public string RawVideoId { get; set; }

		public uint PlayCount { get; set; }

		[DatabaseGenerated(DatabaseGeneratedOption.Computed)]
		public DateTime LastUpdated { get; set; }
	}

	public class SearchHistory
	{
		[Key]
		public string Keyword { get; set; }

		[Key]
		public SearchTarget Target { get; set; }

		public uint SearchCount { get; set; }

		[DatabaseGenerated(DatabaseGeneratedOption.Computed)]
		public DateTime LastUpdated { get; set; }
	}
}
