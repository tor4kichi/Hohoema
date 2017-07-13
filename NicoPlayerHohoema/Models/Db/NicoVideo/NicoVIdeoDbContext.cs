using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Mntone.Nico2;
using Mntone.Nico2.Videos.Comment;
using Mntone.Nico2.Videos.Thumbnail;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models.Db
{
	// Note: NicoVideoから取得できる情報を扱います


	public class NicoVideoDbContext : DbContext
	{
		public DbSet<NicoVideoInfo> VideoInfos { get; set; }
		public DbSet<UserInfo> Users { get; set; }
		public DbSet<NicoVideoComment> Comments { get; set; }

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			var connectionString = new SqliteConnectionStringBuilder { DataSource = "NicoVideo.db" }.ToString();
			optionsBuilder.UseSqlite(new SqliteConnection(connectionString));
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
		}

		public static async Task InitializeAsync()
		{
			using (var db = new NicoVideoDbContext())
			{
				await db.Database.EnsureCreatedAsync();
			}
		}
	}

	


	public class NicoVideoInfo
	{
		[Key]
		public string RawVideoId { get; set; }

		public string ThreadId { get; set; }

		public string VideoId { get; set; }
		public bool IsDeleted { get; set; }

		public string Title { get; set; }
		public string Description { get; set; }
		public string ThumbnailUrl { get; set; }

		public TimeSpan Length { get; set; }
		public DateTime PostedAt { get; set; }
		public MovieType MovieType { get; set; }

		public uint HighSize { get; set; }
		public uint LowSize { get; set; }

		public uint ViewCount { get; set; }
		public uint MylistCount { get; set; }
		public uint CommentCount { get; set; }

		public uint UserId { get; set; }
		public UserType UserType { get; set; }

		public string TagsJson { get; set; }

		public string DescriptionWithHtml { get; set; }

		public PrivateReasonType PrivateReasonType { get; set; }

		public DateTime LastUpdated { get; set; }


		public List<ThumbnailTag> GetTags()
		{
            if (TagsJson == null) { return new List<ThumbnailTag>(); }

			return Newtonsoft.Json.JsonConvert.DeserializeObject<List<ThumbnailTag>>(TagsJson);
		}

		public void SetTags(List<ThumbnailTag> tags)
		{
			TagsJson = Newtonsoft.Json.JsonConvert.SerializeObject(tags);
		}
	}

	


	public class NicoVideoComment
	{
		[Key]
		public string ThreadId { get; set; }

		public uint CommentCount { get; set; }

		public string ChatListJson { get; set; }


		public List<Chat> GetComments()
		{
            if (ChatListJson == null) { return null; }

			return Newtonsoft.Json.JsonConvert.DeserializeObject<List<Chat>>(ChatListJson);
		}

		public void SetComments(List<Chat> comments)
		{
			ChatListJson = Newtonsoft.Json.JsonConvert.SerializeObject(comments);
		}
	}


	public class UserInfo
	{
		[Key]
		public string UserId { get; set; }

		public string Name { get; set; }

		public string IconUri { get; set; }
	}


	
}
