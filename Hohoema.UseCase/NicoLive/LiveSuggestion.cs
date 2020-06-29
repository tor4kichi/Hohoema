using Hohoema.Models.Pages;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Live
{
	public class LiveSuggestion
	{
		public string Title { get; private set; }

		public List<SuggestAction> Actions { get; private set; }

		public LiveSuggestion(string title, params SuggestAction[] actions)
		{
			Title = title;
			Actions = actions.ToList();
		}
	}

	public class SuggestAction
	{
		public string Label { get; private set; }
		public DelegateCommand SuggestActionCommand { get; private set; }

		public SuggestAction(string label, Action action)
		{
			Label = label;
			SuggestActionCommand = new DelegateCommand(action);
		}
	}


	
}
