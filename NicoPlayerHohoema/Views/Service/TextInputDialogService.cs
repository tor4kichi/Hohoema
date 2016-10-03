using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Views.Service
{
	public sealed class TextInputDialogService
	{
		public TextInputDialogService()
		{

		}

		public async Task<string> GetTextAsync(string title, string placeholder, string defaultText = "", Func<string, bool> validater = null)
		{
			if (validater == null)
			{
				validater = EmptyValidater;
			}
			var context = new TextInputDialogContext(title, placeholder, defaultText, validater);

			var dialog = new Views.Service.TextInputDialog()
			{
				DataContext = context
			};

			var result = await dialog.ShowAsync();

			// 仮想入力キーボードを閉じる
			Windows.UI.ViewManagement.InputPane.GetForCurrentView().TryHide();

			if (result == Windows.UI.Xaml.Controls.ContentDialogResult.Secondary)
			{
				return context.GetValidText();
			}
			else
			{
				return null;
			}
		}

		private bool EmptyValidater(string s)
		{
			return true;
		}
	}


	public class TextInputDialogContext
	{
		public TextInputDialogContext(string title, string placeholder, string defaultText, Func<string, bool> validater)
		{
			Title = title;
			PlaceholderText = placeholder;
			Text = new ReactiveProperty<string>(defaultText);
			IsValid = Text.Select(validater)
				.ToReactiveProperty();
		}

		public string Title { get; set; }
		public string PlaceholderText { get; set; }
		public ReactiveProperty<string> Text { get; private set; }

		public ReactiveProperty<bool> IsValid { get; private set; }

		public string GetValidText()
		{
			return Text.Value;
		}


	}
}
