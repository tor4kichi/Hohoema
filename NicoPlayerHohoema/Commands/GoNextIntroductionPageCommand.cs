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
        protected override bool CanExecute(object parameter)
        {
            return true;
        }

        protected override async void Execute(object parameter)
        {
            var pageManager = HohoemaCommnadHelper.GetPageManager();

            var localStorge = new Microsoft.Toolkit.Uwp.Helpers.LocalObjectStorageHelper();

            switch (pageManager.CurrentPageType)
            {
                case Models.HohoemaPageType.PrologueIntroduction:
                    pageManager.OpenPage(Models.HohoemaPageType.NicoAccountIntroduction);
                    break;
                case Models.HohoemaPageType.NicoAccountIntroduction:
                    pageManager.OpenPage(Models.HohoemaPageType.VideoCacheIntroduction);
                    break;
                case Models.HohoemaPageType.VideoCacheIntroduction:
                    pageManager.OpenPage(Models.HohoemaPageType.EpilogueIntroduction);
                    break;
                case Models.HohoemaPageType.EpilogueIntroduction:
                default:
                    // 初回起動の案内が完了したことを記録
#if !DEBUG || false
                    localStorge.Save(App.IS_COMPLETE_INTRODUCTION, true);
#endif
                    // スタートアップページを開く
                    pageManager.OpenStartupPage();

                    // イントロダクション系ページに戻れないようページ履歴を消去
                    await Task.Delay(100);
                    pageManager.ClearNavigateHistory();

                    break;
            }
        }
    }
}
