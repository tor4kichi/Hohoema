using Mntone.Nico2.Videos.Comment;
using NicoPlayerHohoema.Models;
using Prism.Commands;
using Prism.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using NicoPlayerHohoema.Views;
using System.Reactive.Concurrency;
using System.Threading;

namespace NicoPlayerHohoema.ViewModels
{
	public class CommentCommandEditerViewModel : BindableBase
	{
		public PlayerSettings PlayerSettings { get; }
		public NiconicoSession NiconicoSession { get; }

		public CommentCommandEditerViewModel(
			PlayerSettings playerSettings,
			NiconicoSession niconicoSession
			)
		{
			PlayerSettings = playerSettings;
			NiconicoSession = niconicoSession;
		}
	}
}
