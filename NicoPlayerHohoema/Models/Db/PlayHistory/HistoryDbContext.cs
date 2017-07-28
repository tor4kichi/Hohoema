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
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace NicoPlayerHohoema.Models.Db
{
	public sealed class PlayHistoryDbContext : DbContext
	{
		public DbSet<VideoPlayHistory> VideoPlayHistory { get; set; }

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			var connectionString = new SqliteConnectionStringBuilder { DataSource = "PlayHistory.db" }.ToString();
			optionsBuilder.UseSqlite(new SqliteConnection(connectionString));
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
            modelBuilder.Entity<VideoPlayHistory>()
                .HasKey(x => x.RawVideoId);
        }

        

        public static async Task InitializeAsync()
		{
			using (var db = new PlayHistoryDbContext())
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
    
}
