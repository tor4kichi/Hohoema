using NicoPlayerHohoema.Models;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;

namespace NicoPlayerHohoema.Commands
{
    public sealed class OpenFeedSourceCommand : DelegateCommandBase
    {
        protected override bool CanExecute(object parameter)
        {
            return parameter is ViewModels.FeedSourceBookmark;
        }

        protected override void Execute(object parameter)
        {
            if (parameter is ViewModels.FeedSourceBookmark)
            {
                var bookmark = (parameter as ViewModels.FeedSourceBookmark).Bookmark;
                var pageManager = App.Current.Container.Resolve<PageManager>();
                switch (bookmark.BookmarkType)
                {
                    case Database.BookmarkType.SearchWithTag:
                        {
                            var searchPayload = SearchPagePayloadContentHelper.CreateDefault(SearchTarget.Tag, bookmark.Content);
                            pageManager.OpenPage(HohoemaPageType.SearchResultTag, searchPayload.ToParameterString());
                        }
                        break;
                    case Database.BookmarkType.SearchWithKeyword:
                        {
                            var searchPayload = SearchPagePayloadContentHelper.CreateDefault(SearchTarget.Keyword, bookmark.Content);
                            pageManager.OpenPage(HohoemaPageType.SearchResultTag, searchPayload.ToParameterString());
                        }
                        break;
                    case Database.BookmarkType.Mylist:
                        pageManager.OpenPage(HohoemaPageType.Mylist,
                            new MylistPagePayload(bookmark.Content).ToParameterString()
                            );
                        break;
                    case Database.BookmarkType.User:
                        pageManager.OpenPage(HohoemaPageType.UserVideo, bookmark.Content);
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
