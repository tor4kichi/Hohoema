using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reactive.Bindings;
using NicoPlayerHohoema.Models;
using Reactive.Bindings.Extensions;
using Prism.Commands;
using System.Reactive.Linq;

namespace NicoPlayerHohoema.ViewModels
{
	public class CommentSettingsPageContentViewModel : SettingsPageContentViewModel
	{
		public CommentSettingsPageContentViewModel(HohoemaApp hohoemaApp, string title)
			: base(title)
		{
			_HohoemaApp = hohoemaApp;
			_NGSettings = _HohoemaApp.UserSettings.NGSettings;
			_PlayerSettings = _HohoemaApp.UserSettings.PlayerSettings;
			






			// NG Comment User Id
			NGCommentUserIdEnable = _NGSettings.ToReactivePropertyAsSynchronized(x => x.NGCommentUserIdEnable);
			NGCommentUserIds = _NGSettings.NGCommentUserIds
				.ToReadOnlyReactiveCollection(x =>
					RemovableSettingsListItemHelper.UserIdInfoToRemovableListItemVM(x, OnRemoveNGCommentUserIdFromList)
					);

			NGCommentKeywordEnable = _NGSettings.ToReactivePropertyAsSynchronized(x => x.NGCommentKeywordEnable);
			NGCommentKeywords = _NGSettings.NGCommentKeywords.ToReadOnlyReactiveCollection(
				x => new NGKeywordViewModel(x, OnRemoveNGCommentKeyword)
				);


			NGCommentGlassMowerEnable = _NGSettings.ToReactivePropertyAsSynchronized(x => x.NGCommentGlassMowerEnable);


			NGCommentScoreTypes = ((IEnumerable<NGCommentScore>)Enum.GetValues(typeof(NGCommentScore))).ToList();

			SelectedNGCommentScore = _NGSettings.ToReactivePropertyAsSynchronized(x => x.NGCommentScoreType);

			AddNewNGCommentKeywordCommand = new DelegateCommand(() =>
			{
				_NGSettings.NGCommentKeywords.Add(new NGKeyword()
				{
					TestText = "",
					Keyword = ""
				});
			});



			// Comment Display 
			DefaultCommentDisplay = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.DefaultCommentDisplay);
			IncrementReadablityOwnerComment = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.IncrementReadablityOwnerComment);
			CommentRenderingFPS = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.CommentRenderingFPS);
			CommentFontScale = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.DefaultCommentFontScale);

			CommentRenderringFPSList = new List<uint>()
			{
				5, 10, 15, 24, 30, 45, 60, 75, 90, 120
			};

		}




		internal void OnRemoveNGCommentKeyword(NGKeyword keywordInfo)
		{
			_NGSettings.NGCommentKeywords.Remove(keywordInfo);
		}

		private void OnRemoveNGCommentUserIdFromList(string userId)
		{
			var removeTarget = _NGSettings.NGCommentUserIds.First(x => x.UserId == userId);
			_NGSettings.NGCommentUserIds.Remove(removeTarget);
		}


		


		


		public override void OnLeave()
		{
			_NGSettings.Save().ConfigureAwait(false);
		}




		

		public DelegateCommand AddNewNGCommentKeywordCommand { get; private set; }

		public ReactiveProperty<bool> NGCommentUserIdEnable { get; private set; }
		public ReadOnlyReactiveCollection<RemovableListItem<string>> NGCommentUserIds { get; private set; }

		public ReactiveProperty<bool> NGCommentKeywordEnable { get; private set; }
		public ReadOnlyReactiveCollection<NGKeywordViewModel> NGCommentKeywords { get; private set; }

		public ReactiveProperty<bool> NGCommentGlassMowerEnable { get; private set; }

		public List<NGCommentScore> NGCommentScoreTypes { get; private set; }
		public ReactiveProperty<NGCommentScore> SelectedNGCommentScore { get; private set; }



		public ReactiveProperty<bool> DefaultCommentDisplay { get; private set; }
		public ReactiveProperty<bool> IncrementReadablityOwnerComment { get; private set; }
		public ReactiveProperty<uint> CommentRenderingFPS { get; private set; }
		public ReactiveProperty<float> CommentFontScale { get; private set; }

		public List<uint> CommentRenderringFPSList { get; private set; }



		HohoemaApp _HohoemaApp;
		NGSettings _NGSettings;
		PlayerSettings _PlayerSettings;
	}
}
