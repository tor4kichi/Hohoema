using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Ioc;
using Mntone.Nico2.Videos.Ranking;
using NicoPlayerHohoema.Models;
using Prism.Events;
using NicoPlayerHohoema.Services.Helpers;
using I18NPortable;

namespace NicoPlayerHohoema.Services.Commands.Ranking
{
    public class ShowHiddenRankingGenreTagsDialogCommand : DelegateCommandBase
    {
        protected override bool CanExecute(object parameter)
        {
            return true;
        }

        protected override async void Execute(object parameter)
        {
            var eventAggregator = App.Current.Container.Resolve<IEventAggregator>();
            var dialogService = App.Current.Container.Resolve<Services.DialogService>();
            var rankingSettings = App.Current.Container.Resolve<RankingSettings>();

            // タグの内部形式から表記名を引くための準備
            Dictionary<string, (RankingGenre, string)> tagToDisplayNameMap = new Dictionary<string, (RankingGenre, string)>();
            foreach (var genre in Enum.GetValues(typeof(RankingGenre)).Cast<RankingGenre>().Skip(1))
            {
                var genreTags = Database.Local.RankingGenreTagsDb.Get(genre);
                foreach (var genreTag in genreTags?.Tags ?? Enumerable.Empty<Database.Local.RankingGenreTag>())
                {
                    if (string.IsNullOrEmpty(genreTag.Tag)) { continue; }

                    // ジャンルを超えて同一タグが生成されることがある（ASMRなど）
                    if (!tagToDisplayNameMap.ContainsKey(genreTag.Tag))
                    {
                        tagToDisplayNameMap.Add(genreTag.Tag, (genre, genreTag.DisplayName));
                    }
                }
            }

            // 「人気のタグ」から外れたであろうタグを非表示対象から除去する
            foreach (var tag in rankingSettings.HiddenTags.ToArray())
            {
                if (!tagToDisplayNameMap.ContainsKey(tag.Tag))
                {
                    rankingSettings.HiddenTags.Remove(tag);

                    System.Diagnostics.Debug.WriteLine("使用不可なタグをHiddenTagsから除去: " + tag);
                }
            }
            _ = rankingSettings.Save();

            var items = Enumerable.Concat(
                rankingSettings.HiddenGenres.Select(x => new HiddenGenreItem() { Label = x.Translate(), Genre = x }),
                rankingSettings.HiddenTags.Select(x => new HiddenGenreItem() { Label = $"{x.Label} - {x.Genre.Translate()}" , Tag = x.Tag, Genre = x.Genre })
                )
                .ToArray();

            var result = await dialogService.ShowMultiChoiceDialogAsync<HiddenGenreItem>(
                "SelectDisplayRankingGenreOrTag".Translate(), 
                items, Enumerable.Empty<HiddenGenreItem>(),
                nameof(HiddenGenreItem.Label)
                );

            var genreShowRequestEvent = eventAggregator.GetEvent<Events.RankingGenreShowRequestedEvent>();
            foreach (var showGenreOrTag in result ?? Enumerable.Empty<HiddenGenreItem>())
            {
                genreShowRequestEvent.Publish(new Events.RankingGenreCustomizeEventArgs()
                {
                    RankingGenre = showGenreOrTag.Genre,
                    Tag = showGenreOrTag.Tag,
                });
            }
        }

        
    }

    public class HiddenGenreItem
    {
        public string Label { get; set; }
        public RankingGenre Genre { get; set; }
        public string Tag { get; set; }
    }
}
