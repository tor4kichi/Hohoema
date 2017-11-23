using NicoPlayerHohoema.Models;
using Prism.Commands;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels
{
	public class FeedVideoInfoControlViewModel : VideoInfoControlViewModel
	{
		public FeedVideoInfoControlViewModel(Database.NicoVideo nicoVideo, Database.Bookmark sourceBookmark)
			: base(nicoVideo)
		{
            SourceBookmark = sourceBookmark;
		}

        public Database.Bookmark SourceBookmark { get; }

        public Database.BookmarkType SourceType => SourceBookmark.BookmarkType;
        public string SourceLabel => SourceBookmark.Label;

    }

}
