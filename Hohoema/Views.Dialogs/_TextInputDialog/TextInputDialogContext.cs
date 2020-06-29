using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hohoema.Dialogs
{
	public class TextInputDialogContext
	{
        private SynchronizationContextScheduler _CurrentWindowContextScheduler;
        public SynchronizationContextScheduler CurrentWindowContextScheduler
        {
            get
            {
                return _CurrentWindowContextScheduler
                    ?? (_CurrentWindowContextScheduler = new SynchronizationContextScheduler(SynchronizationContext.Current));
            }
        }

        public TextInputDialogContext(string title, string placeholder, string defaultText, Func<string, bool> validater)
		{
			Title = title;
			PlaceholderText = placeholder;
			Text = new ReactiveProperty<string>(CurrentWindowContextScheduler, defaultText);
			IsValid = Text.Select(validater)
				.ToReactiveProperty(CurrentWindowContextScheduler);
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
