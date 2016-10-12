using Prism.Windows.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels.LiveVideoInfoContent
{
	abstract public class LiveInfoContentViewModelBase : ViewModelBase
	{
		virtual public Task OnEnter() { return Task.CompletedTask; }
		virtual public void OnLeave() { }
	}
}
