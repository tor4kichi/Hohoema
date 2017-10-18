using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Dialogs
{
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
