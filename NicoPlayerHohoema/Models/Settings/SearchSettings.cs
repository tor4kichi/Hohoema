using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
	[DataContract]
	public sealed class SearchSeetings : SettingsBase
	{
		public const int MaxHistoryCount = 20;


		public SearchSeetings()
		{
			_SearchHistory = new ObservableCollection<SearchHistoryItem>();

			SearchHistory = new ReadOnlyObservableCollection<SearchHistoryItem>(_SearchHistory);
		}


		[OnDeserialized]
		void OnDeserialized(StreamingContext context)
		{
			SearchHistory = new ReadOnlyObservableCollection<SearchHistoryItem>(_SearchHistory);
		}


		public Task UpdateSearchHistory(string keyword, SearchTarget target)
		{
			// すでに含まれている場合は一旦削除
			RemoveSearchHistory(keyword, target);

			// 先頭に追加
			_SearchHistory.Insert(0, new SearchHistoryItem()
			{
				Keyword = keyword,
				Target = target
			} );

			// 最大数以下になるように古いアイテムを削除
			while(_SearchHistory.Count > MaxHistoryCount)
			{
				_SearchHistory.Remove(_SearchHistory.Last());
			}

			return Save();
		}


		public Task RemoveSearchHistory(string keyword, SearchTarget target, bool withSave = true)
		{
			var alreadItem = _SearchHistory.SingleOrDefault(x => x.Keyword == keyword && x.Target == target);
			if (alreadItem != null)
			{
				_SearchHistory.Remove(alreadItem);
			}

			return withSave ? Save() : Task.CompletedTask;
		}


		public Task RemoveAllSearchHistory()
		{
			_SearchHistory.Clear();

			return Save();
		}


		[DataMember(Name = "history")]
		private ObservableCollection<SearchHistoryItem> _SearchHistory { get; set; }

		public ReadOnlyObservableCollection<SearchHistoryItem> SearchHistory { get; private set; }
	}



	[DataContract]
	public class SearchHistoryItem
	{
		[DataMember]
		public string Keyword { get; set; }

		[DataMember]
		public SearchTarget Target { get; set; } 
	}
}
