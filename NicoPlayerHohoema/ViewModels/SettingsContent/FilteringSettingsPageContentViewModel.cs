using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NicoPlayerHohoema.Models;
using Reactive.Bindings.Extensions;
using Prism.Commands;
using System.Reactive.Linq;
using System.Collections.ObjectModel;
using Reactive.Bindings;
using Prism.Mvvm;
using NicoPlayerHohoema.Views.Service;
using System.Windows.Input;
using System.Text.RegularExpressions;
using Windows.UI.Popups;

namespace NicoPlayerHohoema.ViewModels
{
	public class FilteringSettingsPageContentViewModel : SettingsPageContentViewModel
	{
        public ReactiveProperty<bool> NGVideoIdEnable { get; private set; }
        public ReadOnlyReactiveCollection<RemovableListItem<string>> NGVideoIds { get; private set; }

        public ReactiveProperty<bool> NGVideoOwnerUserIdEnable { get; private set; }
        public ReadOnlyReactiveCollection<UserIdInfo> NGVideoOwnerUserIds { get; private set; }
        public DelegateCommand<UserIdInfo> OpenUserPageCommand { get; }


        public ReactiveProperty<bool> NGVideoTitleKeywordEnable { get; private set; }

        public ReactiveProperty<string> NGVideoTitleKeywords { get; }
        public ReadOnlyReactiveProperty<string> NGVideoTitleKeywordError { get; private set; }



        public ReactiveProperty<bool> NGCommentUserIdEnable { get; private set; }
        public ReadOnlyReactiveCollection<RemovableListItem<string>> NGCommentUserIds { get; private set; }

        public ReactiveProperty<bool> NGCommentKeywordEnable { get; private set; }
        public ReactiveProperty<string> NGCommentKeywords { get; private set; }
        public ReadOnlyReactiveProperty<string> NGCommentKeywordError { get; private set; }

        public List<NGCommentScore> NGCommentScoreTypes { get; private set; }
        public ReactiveProperty<NGCommentScore> SelectedNGCommentScore { get; private set; }


        public ReactiveProperty<bool> CommentGlassMowerEnable { get; private set; }




        NGSettings _NGSettings;
        RankingSettings _RankingSettings;
        HohoemaApp _HohoemaApp;





        public FilteringSettingsPageContentViewModel(HohoemaApp hohoemaApp, PageManager pageManager)
			: base("フィルタ", HohoemaSettingsKind.Filtering)
		{
			_HohoemaApp = hohoemaApp;
			_NGSettings = _HohoemaApp.UserSettings.NGSettings;
			_RankingSettings = _HohoemaApp.UserSettings.RankingSettings;

            
			// NG Video


			// NG Video
			NGVideoIdEnable = _NGSettings.ToReactivePropertyAsSynchronized(x => x.NGVideoIdEnable);
			NGVideoIds = _NGSettings.NGVideoIds
				.ToReadOnlyReactiveCollection(x =>
					RemovableSettingsListItemHelper.VideoIdInfoToRemovableListItemVM(x, OnRemoveNGVideoIdFromList)
					);

			// NG Video Owner User Id
			NGVideoOwnerUserIdEnable = _NGSettings.ToReactivePropertyAsSynchronized(x => x.NGVideoOwnerUserIdEnable);
			NGVideoOwnerUserIds = _NGSettings.NGVideoOwnerUserIds
				.ToReadOnlyReactiveCollection();

            OpenUserPageCommand = new DelegateCommand<UserIdInfo>(userIdInfo => 
            {
                pageManager.OpenPage(HohoemaPageType.UserInfo, userIdInfo.UserId);
            });

            // NG Keyword on Video Title
            NGVideoTitleKeywordEnable = _NGSettings.ToReactivePropertyAsSynchronized(x => x.NGVideoTitleKeywordEnable);
            NGVideoTitleKeywords = new ReactiveProperty<string>(string.Empty);
            NGVideoTitleKeywordError = NGVideoTitleKeywords
                .Select(x =>
                {
                    var keywords = x.Split('\r');
                    var invalidRegex = keywords.FirstOrDefault(keyword =>
                    {
                        Regex regex = null;
                        try
                        {
                            regex = new Regex(keyword);
                        }
                        catch { }
                        return regex == null;
                    });

                    if (invalidRegex == null)
                    {
                        return string.Empty;
                    }
                    else
                    {
                        return $"Error in \"{invalidRegex}\"";
                    }
                })
                .ToReadOnlyReactiveProperty();
            // NG動画タイトルキーワードを追加するコマンド


            NGCommentUserIdEnable = _NGSettings.ToReactivePropertyAsSynchronized(x => x.NGCommentUserIdEnable);
            NGCommentUserIds = _NGSettings.NGCommentUserIds
                .ToReadOnlyReactiveCollection(x =>
                    RemovableSettingsListItemHelper.UserIdInfoToRemovableListItemVM(x, OnRemoveNGCommentUserIdFromList)
                    );

            NGCommentKeywordEnable = _NGSettings.ToReactivePropertyAsSynchronized(x => x.NGCommentKeywordEnable);
            NGCommentKeywords = new ReactiveProperty<string>(string.Empty);

            NGCommentKeywordError = NGCommentKeywords
                .Select(x =>
                {
                    var keywords = x.Split('\r');
                    var invalidRegex = keywords.FirstOrDefault(keyword =>
                    {
                        Regex regex = null;
                        try
                        {
                            regex = new Regex(keyword);
                        }
                        catch { }
                        return regex == null;
                    });

                    if (invalidRegex == null)
                    {
                        return string.Empty;
                    }
                    else
                    {
                        return $"Error in \"{invalidRegex}\"";
                    }
                })
                .ToReadOnlyReactiveProperty();

            NGCommentScoreTypes = ((IEnumerable<NGCommentScore>)Enum.GetValues(typeof(NGCommentScore))).ToList();

            SelectedNGCommentScore = _NGSettings.ToReactivePropertyAsSynchronized(x => x.NGCommentScoreType);



            CommentGlassMowerEnable = _HohoemaApp.UserSettings.PlayerSettings
                .ToReactivePropertyAsSynchronized(x => x.CommentGlassMowerEnable);
        }

        protected override void OnEnter(ICollection<IDisposable> focusingDispsable)
        {
            NGVideoTitleKeywords.Value = string.Join("\r", _NGSettings.NGVideoTitleKeywords.Select(x => x.Keyword)) + "\r";
            NGCommentKeywords.Value = string.Join("\r", _NGSettings.NGCommentKeywords.Select(x => x.Keyword)) + "\r";

            base.OnEnter(focusingDispsable);
        }

        protected override void OnLeave()
        {
            // NG VideoTitleを複数行NG動画タイトル文字列から再構成
            _NGSettings.NGVideoTitleKeywords.Clear();
            foreach (var ngKeyword in NGVideoTitleKeywords.Value.Split('\r'))
            {
                if (!string.IsNullOrWhiteSpace(ngKeyword))
                {
                    _NGSettings.NGVideoTitleKeywords.Add(new NGKeyword() { Keyword = ngKeyword });
                }
            }

            // NG Comments
            _NGSettings.NGCommentKeywords.Clear();
            foreach (var ngKeyword in NGCommentKeywords.Value.Split('\r'))
            {
                if (!string.IsNullOrWhiteSpace(ngKeyword))
                {
                    _NGSettings.NGCommentKeywords.Add(new NGKeyword() { Keyword = ngKeyword });
                }
            }

            _NGSettings.Save().ConfigureAwait(false);


            _RankingSettings.GetFile().ContinueWith(async prevTask => 
            {
                await HohoemaApp.PushToRoamingData(prevTask.Result);
            });
            _NGSettings.GetFile().ContinueWith(async prevTask =>
            {
                await HohoemaApp.PushToRoamingData(prevTask.Result);
            });
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






		

		private void OnRemoveNGVideoIdFromList(string videoId)
		{
			var removeTarget = _NGSettings.NGVideoIds.First(x => x.VideoId == videoId);
			_NGSettings.NGVideoIds.Remove(removeTarget);
		}


		private void OnRemoveNGVideoOwnerUserIdFromList(string userId)
		{
			var removeTarget = _NGSettings.NGVideoOwnerUserIds.First(x => x.UserId == userId);
			_NGSettings.NGVideoOwnerUserIds.Remove(removeTarget);
		}

		internal async void OnRemoveNGTitleKeyword(NGKeyword keywordInfo)
		{
            var dialog = new MessageDialog($"NGタイトルを削除しますか？", $"『{keywordInfo.Keyword}』のNGタイトル削除");
            dialog.Commands.Add(new UICommand() { Label = "キャンセル", Id = "cancel" });
            dialog.Commands.Add(new UICommand() { Label = "削除", Id = "delete" });

            var result = await dialog.ShowAsync();

            if ((result.Id as string) == "delete")
            {
                _NGSettings.NGVideoTitleKeywords.Remove(keywordInfo);
            }
        }


		
	}
	
}
