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
		public SearchSeetings()
		{
			_SearchHistory = new ObservableCollection<string>();

			SearchHistory = new ReadOnlyObservableCollection<string>(_SearchHistory);
		}


		[OnDeserialized]
		void OnDeserialized(StreamingContext context)
		{
			SearchHistory = new ReadOnlyObservableCollection<string>(_SearchHistory);
		}


		public void UpdateSearchHistory(string keyword)
		{
			// すでに含まれている場合は一旦削除
			RemoveSearchHistory(keyword);

			// 先頭に追加
			_SearchHistory.Insert(0, keyword);

			Save().ConfigureAwait(false);
		}


		public void RemoveSearchHistory(string keyword)
		{
			if (_SearchHistory.Contains(keyword))
			{
				_SearchHistory.Remove(keyword);
			}

			Save().ConfigureAwait(false);
		}


		[DataMember(Name = "history")]
		private ObservableCollection<string> _SearchHistory { get; set; }

		public ReadOnlyObservableCollection<string> SearchHistory { get; private set; }
	}
}
