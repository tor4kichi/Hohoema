using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Services.Page
{
    public sealed class PreventBackNavigationOnShowPlayerService
    {
        public PreventBackNavigationOnShowPlayerService(
            PlayerViewManager playerViewManager,
            PageManager pageManager
            )
        {
            PlayerViewManager = playerViewManager;
            PageManager = pageManager;

            // メインウィンドウでプレイヤー表示中かつ、小窓状態ではない時に
            // 戻るナビゲーションを行なわないよう設定する
            new[] {
                PlayerViewManager.ObserveProperty(x => x.IsPlayingWithPrimaryView),
                PlayerViewManager.ObserveProperty(x => x.IsPlayerSmallWindowModeEnabled).Select(x => !x)
            }
                .CombineLatestValuesAreAllTrue()
                .Subscribe(preventGoBack => PageManager.PreventBackNavigation = preventGoBack);
        }

        public PlayerViewManager PlayerViewManager { get; }
        public PageManager PageManager { get; }
    }
}
