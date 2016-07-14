using NicoPlayerHohoema.Models;
using Prism.Windows.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Windows.Navigation;
using Windows.UI.Xaml;
using Windows.Storage;

namespace NicoPlayerHohoema.ViewModels
{
	public class AboutPageViewModel : HohoemaViewModelBase
	{
		public AboutPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager)
			: base(hohoemaApp, pageManager)
		{
			
		}


		public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			var dispatcher = Window.Current.CoreWindow.Dispatcher;
			LisenceSummary.Load()
				.ContinueWith(async prevResult =>
				{
					await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
					{
						var lisenceSummary = prevResult.Result;

						LisenceItems = lisenceSummary.Items
							.OrderBy(x => x.Name)
							.Select(x => new LisenceItemViewModel(x))
							.ToList();
						OnPropertyChanged(nameof(LisenceItems));
					});
				});
			base.OnNavigatedTo(e, viewModelState);
		}

		public List<LisenceItemViewModel> LisenceItems { get; private set; }
	}



	public class LisenceItemViewModel
	{
		public LisenceItemViewModel(LisenceItem item)
		{
			Name = item.Name;
			Site = item.Site;
			Authors = item.Authors.ToList();
			LisenceType = LisenceTypeToText(item.LisenceType.Value);
			LisencePageUrl = item.LisencePageUrl;
		}

		public string Name { get; private set; }
		public Uri Site { get; private set; }
		public List<string> Authors { get; private set; }
		public string LisenceType { get; private set; }
		public Uri LisencePageUrl { get; private set; }

		string _LisenceText;
		public string LisenceText
		{
			get
			{
				return _LisenceText
					?? (_LisenceText = LoadLisenceText());
			}
		}

		string LoadLisenceText()
		{
			string path = Windows.ApplicationModel.Package.Current.InstalledLocation.Path + "\\Assets\\LibLisencies\\" + Name + ".txt";
			
			try
			{
				var file = StorageFile.GetFileFromPathAsync(path).AsTask();

				file.Wait(3000);

				var task = FileIO.ReadTextAsync(file.Result).AsTask();
				task.Wait(1000);
				return task.Result;
			}
			catch
			{
				return "";
			}
		}


		private string LisenceTypeToText(LisenceType type)
		{
			switch (type)
			{
				case Models.LisenceType.MIT:
					return "MIT";
				case Models.LisenceType.MS_PL:
					return "Microsoft Public Lisence";
				case Models.LisenceType.Apache_v2:
					return "Apache Lisence version 2.0";
				case Models.LisenceType.Simplified_BSD:
					return "二条項BSDライセンス";
				default:
					throw new NotSupportedException(type.ToString());
			}
		}

	}



}
