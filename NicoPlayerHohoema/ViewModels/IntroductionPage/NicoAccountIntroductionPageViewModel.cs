using NicoPlayerHohoema.Models;
using Prism.Mvvm;
using Prism.Navigation;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NicoPlayerHohoema.ViewModels
{
    public sealed class NicoAccountIntroductionPageViewModel : BindableBase, Prism.Navigation.INavigationAware
    {
        public NicoAccountIntroductionPageViewModel(
            NiconicoSession niconicoSession,
            Commands.GoNextIntroductionPageCommand goNextIntroduction
            )
        {
            NiconicoSession = niconicoSession;
            GoNextIntroduction = goNextIntroduction;
            IsLoggedIn = NiconicoSession.ObserveProperty(x => x.IsLoggedIn)
                .ToReadOnlyReactiveProperty();
        }

        public ReadOnlyReactiveProperty<bool> IsLoggedIn { get; }
        public NiconicoSession NiconicoSession { get; }
        public ICommand GoNextIntroduction { get; }

        CompositeDisposable disposables;


        public void OnNavigatedFrom(INavigationParameters parameters)
        {
            disposables?.Dispose();
            disposables = null;
        }

        public void OnNavigatedTo(INavigationParameters parameters)
        {
            disposables?.Dispose();
            disposables = new CompositeDisposable();

            IsLoggedIn.Where(x => x)
                .Take(1)
                .Delay(TimeSpan.FromSeconds(2.5)) /* ここでログイン確認後の遷移前タメ時間を調整 */
                .Subscribe(_ =>
                {
                    GoNextIntroduction.Execute(null);
                });

            try
            {
                _ = NiconicoSession.SignInWithPrimaryAccount();
            }
            catch { }

        }

        public void OnNavigatingTo(INavigationParameters parameters)
        {
            
        }
    }
}
