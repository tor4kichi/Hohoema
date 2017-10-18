using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace NicoPlayerHohoema.Helpers
{
	public static class ConnectionRetryUtil
	{
		public static async Task<T> TaskWithRetry<T>(Func<Task<T>> func, uint retryCount = 3, int retryInterval = 100)
		{
			int currentRetry = 0;

			Exception lastError = null;
			for (;;)
			{
				try
				{
					// Calling external service.
					return await func();
				}
				catch (Exception ex)
				{
					currentRetry++;

					// Check if the exception thrown was a transient exception
					// based on the logic in the error detection strategy.
					// Determine whether to retry the operation, as well as how 
					// long to wait, based on the retry strategy.
					if (currentRetry > retryCount || !IsTransient(ex))
					{
						// If this is not a transient error 
						// or we should not retry re-throw the exception. 
						throw;
					}

					lastError = ex;

					await Task.Delay(retryInterval);
				}

				// Wait to retry the operation.
				// Consider calculating an exponential delay here and 
				// using a strategy best suited for the operation and fault.
				
			
			}

			throw new Exception("connection retry rimit.", lastError);
		}


		private static bool IsTransient(Exception ex)
		{
			// Determine if the exception is transient.
			// In some cases this may be as simple as checking the exception type, in other 
			// cases it may be necessary to inspect other properties of the exception.
	//		if (ex is OperationTransientException)
	//			return true;

			var webException = ex as WebException;
			if (webException != null)
			{
				// If the web exception contains one of the following status values 
				// it may be transient.
				return new[] {WebExceptionStatus.ConnectionClosed,
				  WebExceptionStatus.Timeout,
				  WebExceptionStatus.RequestCanceled }.
						Contains(webException.Status);
			}

			// Additional exception checking logic goes here.
			return false;
		}
	}
}
