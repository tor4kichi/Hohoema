using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Hohoema.Services
{
    public sealed class NoUIProcessScreenContext : BindableBase, IDisposable
    {
		public NoUIProcessScreenContext()
		{
		}

		public void Dispose()
		{
		}

		private bool _NowWorking;
		public bool NowWorking
		{
			get { return _NowWorking; }
			private set { SetProperty(ref _NowWorking, value); }
		}

		private string _WorkTitle;
		public string WorkTitle
		{
			get { return _WorkTitle; }
			private set { SetProperty(ref _WorkTitle, value); }
		}

		private int _WorkCount;
		public int WorkCount
		{
			get { return _WorkCount; }
			private set { SetProperty(ref _WorkCount, value); }
		}


		private int _WorkTotalCount;
		public int WorkTotalCount
		{
			get { return _WorkTotalCount; }
			private set { SetProperty(ref _WorkTotalCount, value); }
		}



		public async Task StartNoUIWork(string title, Func<IAsyncAction> actionFactory, CancellationToken cancellationToken = default)
		{
			WorkTitle = title;
			WorkTotalCount = 0;
			NowWorking = true;
			try
			{
				await actionFactory().AsTask(cancellationToken);

				await Task.Delay(500);

				cancellationToken.ThrowIfCancellationRequested();
			}
			finally
			{
				NowWorking = false;
			}
		}


		public async Task StartNoUIWork(string title, int totalCount, Func<IAsyncActionWithProgress<int>> actionFactory)
		{
			WorkTitle = title;
			WorkTotalCount = totalCount;
			NowWorking = true;

			var progressHandler = new Progress<int>((x) => WorkCount = x);

			try
			{
				using (var cancelSource = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
				{
					await actionFactory().AsTask(cancelSource.Token, progressHandler);

					await Task.Delay(500);

					cancelSource.Token.ThrowIfCancellationRequested();
				}
			}
			finally
			{
				NowWorking = false;
			}
		}
	}
}
