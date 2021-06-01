
using NiconicoToolkit.Ranking.Video;
using Prism.Mvvm;

namespace Hohoema.Presentation.ViewModels.Niconico.Ranking
{
    public class RankingItem : BindableBase
    {
        public string Label { get; set; }

        public RankingGenre? Genre { get; set; }
        public string Tag { get; set; }

        private bool _IsFavorite;
        public bool IsFavorite
        {
            get { return _IsFavorite; }
            set { SetProperty(ref _IsFavorite, value); }
        }
    }
}
