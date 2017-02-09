using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace NicoPlayerHohoema.ViewModels
{
	public sealed class AppDisplaySettingsPageContentViewModel : SettingsPageContentViewModel
	{
		public static List<string> ThemeList { get; private set; } =
			Enum.GetValues(typeof(ApplicationTheme)).Cast<ApplicationTheme>()
			.Select(x => x.ToString())
			.ToList();

		public ReactiveProperty<string> SelectedApplicationTheme { get; private set; }
		public static bool ThemeChanged { get; private set; } = false;

		public AppDisplaySettingsPageContentViewModel(Views.Service.ToastNotificationService toastService) 
			: base("アプリのUI", HohoemaSettingsKind.AppDisplay)
		{

			var currentTheme = App.GetTheme();
			SelectedApplicationTheme = new ReactiveProperty<string>(currentTheme.ToString(), mode:ReactivePropertyMode.DistinctUntilChanged);

			SelectedApplicationTheme.Subscribe(x => 
			{
				var type = (ApplicationTheme)Enum.Parse(typeof(ApplicationTheme), x);
				App.SetTheme(type);

				if (!ThemeChanged)
				{
					toastService.ShowText("Hohoemaを再起動するとテーマが適用されます。", "");
				}

				ThemeChanged = true;
				OnPropertyChanged(nameof(ThemeChanged));
			});
		}



		public override void OnEnter()
		{
			base.OnEnter();
		}

		public override void OnLeave()
		{
		}

	}
}
