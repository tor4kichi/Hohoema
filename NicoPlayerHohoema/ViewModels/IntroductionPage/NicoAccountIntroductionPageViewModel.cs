using NicoPlayerHohoema.Models;
using Prism.Windows.Mvvm;
using Prism.Windows.Navigation;
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
    public sealed class NicoAccountIntroductionPageViewModel : ViewModelBase
    {
        public NicoAccountIntroductionPageViewModel(
            NiconicoSession niconicoSession
            )
        {
            NiconicoSession = niconicoSession;

            IsLoggedIn = NiconicoSession.ObserveProperty(x => x.IsLoggedIn)
                .ToReadOnlyReactiveProperty();
        }

        public ReadOnlyReactiveProperty<bool> IsLoggedIn { get; }
        public NiconicoSession NiconicoSession { get; }

        CompositeDisposable disposables;

        

        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            disposables?.Dispose();
            disposables = new CompositeDisposable();

            IsLoggedIn.Where(x => x)
                .Take(1)
                .Delay(TimeSpan.FromSeconds(2.5)) /* ここでログイン確認後の遷移前タメ時間を調整 */
                .Subscribe(_ => 
                {
                    var goNextCommand = new Commands.GoNextIntroductionPageCommand() as ICommand;
                    if (goNextCommand != null)
                    {
                        goNextCommand.Execute(null);
                    }
                });

            try
            {
                _ = NiconicoSession.SignInWithPrimaryAccount();
            }
            catch { }

            base.OnNavigatedTo(e, viewModelState);
        }

        public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
        {
            disposables?.Dispose();
            disposables = null;

            base.OnNavigatingFrom(e, viewModelState, suspending);
        }
    }
}
