using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Views.Controls;
using Prism.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;
using Prism.Windows.Mvvm;
using Windows.UI.Xaml.Navigation;
using Prism.Commands;
using NicoPlayerHohoema.Models.Live;
using Windows.UI.ViewManagement;
using Windows.UI.Core;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Metadata;
using Prism.Windows.Navigation;
using System.Reactive.Concurrency;
using NicoPlayerHohoema.Services;
using NicoPlayerHohoema.Services.Page;

namespace NicoPlayerHohoema.ViewModels
{
    public class PlayerWithPageContainerViewModel : BindableBase
    {
        public PlayerWithPageContainerViewModel(
            PlayerViewManager playerViewManager
            )
        {
            PlayerViewManager = playerViewManager;


        }

        public PlayerViewManager PlayerViewManager { get; }

        private Dictionary<string, object> viewModelState = new Dictionary<string, object>();


        /// <summary>
        /// Playerの小窓状態の変化を遅延させて伝播させます、
        /// 
        /// 遅延させている理由は、Player上のFlyoutを閉じる前にリサイズが発生するとFlyoutが
        /// ゴースト化（FlyoutのUIは表示されず閉じれないが、Visible状態と同じようなインタラクションだけは出来てしまう）
        /// してしまうためです。（タブレット端末で発生、PCだと発生確認されず）
        /// この問題を回避するためにFlyoutが閉じられた後にプレイヤーリサイズが走るように遅延を挟んでいます。
        /// </summary>
        //public ReadOnlyReactiveProperty<bool> IsPlayerSmallWindowModeEnabled_DelayedRead { get; private set; }
    }

}
