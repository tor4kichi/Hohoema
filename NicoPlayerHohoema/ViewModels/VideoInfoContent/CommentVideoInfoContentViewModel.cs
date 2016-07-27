using Mntone.Nico2.Videos.Comment;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Views;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels.VideoInfoContent
{
	public class CommentVideoInfoContentViewModel : MediaInfoViewModel
	{
		public CommentVideoInfoContentViewModel(HohoemaUserSettings settings, ObservableCollection<Comment> comments)
			: base("コメント")
		{
			UserSettings = settings;
			Comments = comments;
			IsCommentListScrollWithVideo = new ReactiveProperty<bool>(false);
		}


		public void UpdatePlayPosition(uint videoPosition)
		{
			if (IsCommentListScrollWithVideo.Value)
			{
				// 表示位置の更新
			}
		}

		public ReactiveProperty<bool> IsCommentListScrollWithVideo { get; private set; }

		public HohoemaUserSettings UserSettings { get; private set; }
		public ObservableCollection<Comment> Comments { get; private set; }
	}
}
