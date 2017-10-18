using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Helpers
{
	public static class TaskExtention 
	{
		public static async Task WaitToCompelation(this Task task, int interval = 100, int count = 10)
		{
			for (int i = 0; i < count; i++)
			{
				if (task == null || task.IsCanceled || task.IsCompleted || task.IsFaulted)
				{
					break;
				}

				await Task.Delay(interval).ConfigureAwait(false);
			}
		}
	}
}
