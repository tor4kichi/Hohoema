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
using Windows.UI;

namespace NicoPlayerHohoema.ViewModels
{
	public class CommentSettingsPageContentViewModel : SettingsPageContentViewModel
	{
		static CommentSettingsPageContentViewModel()
		{
			CommentRenderringFPSList = new List<uint>()
			{
				5, 10, 15, 24, 30, 45, 60, 75, 90, 120
			};

			CommentColorList = new List<Color>()
			{
				Colors.WhiteSmoke,
				Colors.Black,
			};
		}

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
			CommentColor = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.CommentColor);
			IsPauseWithCommentWriting = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.PauseWithCommentWriting);
			CommentRenderingFPS = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.CommentRenderingFPS);
			CommentDisplayDuration = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.CommentDisplayDuration, x => x.TotalSeconds, x => TimeSpan.FromSeconds(x));
			CommentFontScale = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.DefaultCommentFontScale);
			CommentGlassMowerEnable = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.CommentGlassMowerEnable);
			IsDefaultCommentWithAnonymous = _PlayerSettings.ToReactivePropertyAsSynchronized(x => x.IsDefaultCommentWithAnonymous);

			IsEnableOwnerCommentCommand = new ReactiveProperty<bool>(_PlayerSettings.CommentCommandPermission.HasFlag(CommentCommandPermissionType.Owner));
			IsEnableUserCommentCommand = new ReactiveProperty<bool>(_PlayerSettings.CommentCommandPermission.HasFlag(CommentCommandPermissionType.User));
			IsEnableAnonymousCommentCommand = new ReactiveProperty<bool>(_PlayerSettings.CommentCommandPermission.HasFlag(CommentCommandPermissionType.Anonymous));

			IsEnableOwnerCommentCommand.Subscribe(x => SetCommentCommandPermission(x, CommentCommandPermissionType.Owner));
			IsEnableUserCommentCommand.Subscribe(x => SetCommentCommandPermission(x, CommentCommandPermissionType.User));
			IsEnableAnonymousCommentCommand.Subscribe(x => SetCommentCommandPermission(x, CommentCommandPermissionType.Anonymous));

		}


		public override void OnLeave()
		{
			_NGSettings.Save().ConfigureAwait(false);
			_PlayerSettings.Save().ConfigureAwait(false);
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





		private void SetCommentCommandPermission(bool isEnable, CommentCommandPermissionType type)
		{
			if (isEnable)
			{
				_PlayerSettings.CommentCommandPermission |= type;
			}
			else
			{
				_PlayerSettings.CommentCommandPermission = _PlayerSettings.CommentCommandPermission & ~type;
			}
		}








		public DelegateCommand AddNewNGCommentKeywordCommand { get; private set; }

		public ReactiveProperty<bool> NGCommentUserIdEnable { get; private set; }
		public ReadOnlyReactiveCollection<RemovableListItem<string>> NGCommentUserIds { get; private set; }

		public ReactiveProperty<bool> NGCommentKeywordEnable { get; private set; }
		public ReadOnlyReactiveCollection<NGKeywordViewModel> NGCommentKeywords { get; private set; }

		public ReactiveProperty<bool> CommentGlassMowerEnable { get; private set; }

		public List<NGCommentScore> NGCommentScoreTypes { get; private set; }
		public ReactiveProperty<NGCommentScore> SelectedNGCommentScore { get; private set; }



		public ReactiveProperty<bool> IsDefaultCommentWithAnonymous { get; private set; }
		public ReactiveProperty<bool> DefaultCommentDisplay { get; private set; }
		public ReactiveProperty<uint> CommentRenderingFPS { get; private set; }
		public ReactiveProperty<double> CommentDisplayDuration { get; private set; }
		public ReactiveProperty<double> CommentFontScale { get; private set; }
		public ReactiveProperty<Color> CommentColor { get; private set; }
		public ReactiveProperty<bool> IsPauseWithCommentWriting { get; private set; }

		public static List<Color> CommentColorList { get; private set; }
		public static List<uint> CommentRenderringFPSList { get; private set; }


		public ReactiveProperty<bool> IsEnableOwnerCommentCommand { get; private set; }
		public ReactiveProperty<bool> IsEnableUserCommentCommand { get; private set; }
		public ReactiveProperty<bool> IsEnableAnonymousCommentCommand { get; private set; }



		HohoemaApp _HohoemaApp;
		NGSettings _NGSettings;
		PlayerSettings _PlayerSettings;
	}
}
