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
//		public DbSet<VideoPlayHistory> VideoPlayHistory { get; set; }
		public DbSet<SearchHistory> SearchHistory { get; set; }

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			var connectionString = new SqliteConnectionStringBuilder { DataSource = "History.db" }.ToString();
			optionsBuilder.UseSqlite(new SqliteConnection(connectionString));
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<SearchHistory>()
				.HasKey(x => new { x.Keyword, x.Target });
		}

		public static async Task InitializeAsync()
		{
			using (var db = new HistoryDbContext())
			{
				await db.Database.EnsureCreatedAsync();
			}
		}
	}


	public class VideoPlayHistory
	{
		[Key]
		public string RawVideoId { get; set; }

		public uint PlayCount { get; set; }

		public DateTime LastPlayed { get; set; }
	}

	public class SearchHistory
	{
		public string Keyword { get; set; }

		public SearchTarget Target { get; set; }

		public uint SearchCount { get; set; }

		public DateTime LastUpdated { get; set; }
	}
}
