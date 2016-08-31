using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels.VideoInfoContent
{
	abstract public class MediaInfoViewModel : BindableBase, IDisposable
	{
		protected CompositeDisposable _CompositeDisposable;

		public MediaInfoViewModel()
		{
			_CompositeDisposable = new CompositeDisposable();
		}

		protected virtual void OnDispose() { }

		public void Dispose()
		{
			OnDispose();

			_CompositeDisposable?.Dispose();
		}
	}
}
