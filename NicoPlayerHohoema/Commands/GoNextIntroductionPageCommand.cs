using NicoPlayerHohoema.Services;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Commands
{
    public sealed class GoNextIntroductionPageCommand : DelegateCommandBase
    {
        public GoNextIntroductionPageCommand(PageManager pageManager)
        {
            PageManager = pageManager;
        }

        public PageManager PageManager { get; }

        protected override bool CanExecute(object parameter)
        {
            return true;
        }

        protected override async void Execute(object parameter)
        {
            var localStorge = new Microsoft.Toolkit.Uwp.Helpers.LocalObjectStorageHelper();

            switch (PageManager.CurrentPageType)
            {
                case Models.HohoemaPageType.PrologueIntroduction:
                    PageManager.OpenPage(Models.HohoemaPageType.NicoAccountIntroduction);
                    break;
                case Models.HohoemaPageType.NicoAccountIntroduction:
                    PageManager.OpenPage(Models.HohoemaPageType.VideoCacheIntroduction);
                    break;
                case Models.HohoemaPageType.VideoCacheIntroduction:
                    PageManager.OpenPage(Models.HohoemaPageType.EpilogueIntroduction);
                    break;
                case Models.HohoemaPageType.EpilogueIntroduction:
                default:
                    // 初回起動の案内が完了したことを記録
                    localStorge.Save(App.IS_COMPLETE_INTRODUCTION, true);

                    // スタートアップページを開く
                    PageManager.OpenStartupPage();

                    // イントロダクション系ページに戻れないようページ履歴を消去
                    await Task.Delay(100);
                    PageManager.ClearNavigateHistory();

                    break;
            }
        }
    }
}
