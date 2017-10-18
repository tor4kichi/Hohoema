using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Dialogs
{
    public struct ContentSelectDialogDefaultSet
	{
		public string DialogTitle { get; set; }
		public string ChoiceListTitle { get; set; }
		public List<SelectDialogPayload> ChoiceList { get; set; }

		public string TextInputTitle { get; set; }
		public Func<string, Task<List<SelectDialogPayload>>> GenerateCandiateList { get; set; }
	}
}
