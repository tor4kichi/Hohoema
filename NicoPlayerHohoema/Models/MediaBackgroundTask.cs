using BackgroundAudioShared;
using BackgroundAudioShared.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Media.Playback;
using Windows.UI.Core;
using Windows.UI.Xaml;

// see@ https://github.com/Microsoft/Windows-universal-samples/blob/master/Samples/BackgroundAudio/cs/BackgroundAudio/Scenario1.xaml.cs

namespace NicoPlayerHohoema.Models
{
	public class MediaBackgroundTask : IDisposable
	{
		const int RPC_S_SERVER_UNAVAILABLE = -2147023174; // 0x800706BA

		private AutoResetEvent backgroundAudioTaskStarted;
		private bool _isMyBackgroundTaskRunning = false;


		private MediaBackgroundTask()
		{
			// Setup the initialization lock
			backgroundAudioTaskStarted = new AutoResetEvent(false);
		}

		public static MediaBackgroundTask Create()
		{
			var task = new MediaBackgroundTask();

			// #background
			// Adding App suspension handlers here so that we can unsubscribe handlers 
			// that access BackgroundMediaPlayer events
			Application.Current.Suspending += task.ForegroundApp_Suspending;
			Application.Current.Resuming += task.ForegroundApp_Resuming;
			ApplicationSettingsHelper.SaveSettingsValue(ApplicationSettingsConstants.AppState, AppState.Active.ToString());

			task.StartBackgroundAudioTask();

			return task;
		}


		/// <summary>
		/// You should never cache the MediaPlayer and always call Current. It is possible
		/// for the background task to go away for several different reasons. When it does
		/// an RPC_S_SERVER_UNAVAILABLE error is thrown. We need to reset the foreground state
		/// and restart the background task.
		/// </summary>
		private MediaPlayer CurrentPlayer
		{
			get
			{
				MediaPlayer mp = null;
				int retryCount = 2;

				while (mp == null && --retryCount >= 0)
				{
					try
					{
						mp = BackgroundMediaPlayer.Current;
					}
					catch (Exception ex)
					{
						if (ex.HResult == RPC_S_SERVER_UNAVAILABLE)
						{
							// The foreground app uses RPC to communicate with the background process.
							// If the background process crashes or is killed for any reason RPC_S_SERVER_UNAVAILABLE
							// is returned when calling Current. We must restart the task, the while loop will retry to set mp.
							ResetAfterLostBackground();
							StartBackgroundAudioTask();
						}
						else
						{
							throw;
						}
					}
				}

				if (mp == null)
				{
					throw new Exception("Failed to get a MediaPlayer instance.");
				}

				return mp;
			}
		}

		/// <summary>
		/// Gets the information about background task is running or not by reading the setting saved by background task.
		/// This is used to determine when to start the task and also when to avoid sending messages.
		/// </summary>
		private bool IsMyBackgroundTaskRunning
		{
			get
			{
				if (_isMyBackgroundTaskRunning)
					return true;

				string value = ApplicationSettingsHelper.ReadResetSettingsValue(ApplicationSettingsConstants.BackgroundTaskState) as string;
				if (value == null)
				{
					return false;
				}
				else
				{
					try
					{
						_isMyBackgroundTaskRunning = EnumHelper.Parse<BackgroundTaskState>(value) == BackgroundTaskState.Running;
					}
					catch (ArgumentException)
					{
						_isMyBackgroundTaskRunning = false;
					}
					return _isMyBackgroundTaskRunning;
				}
			}
		}








		public void Dispose()
		{
			// background
			if (IsMyBackgroundTaskRunning)
			{
				RemoveMediaPlayerEventHandlers();
				ApplicationSettingsHelper.ReadResetSettingsValue(ApplicationSettingsConstants.BackgroundTaskState);
			}

			Application.Current.Suspending -= ForegroundApp_Suspending;
			Application.Current.Resuming -= ForegroundApp_Resuming;

		}









		private void UpdateTransportControls(MediaPlayerState state)
		{
			if (state == MediaPlayerState.Playing)
			{

			}
			else
			{

			}
		}


		#region Foreground App Lifecycle Handlers


		/// <summary>
		/// Read persisted current track information from application settings
		/// </summary>
		private string GetCurrentTrackIdAfterAppResume()
		{
			object value = ApplicationSettingsHelper.ReadResetSettingsValue(ApplicationSettingsConstants.VideoId);
			if (value != null)
				return (String)value;
			else
				return null;
		}

		/// <summary>
		/// Sends message to background informing app has resumed
		/// Subscribe to MediaPlayer events
		/// </summary>
		void ForegroundApp_Resuming(object sender, object e)
		{
			ApplicationSettingsHelper.SaveSettingsValue(ApplicationSettingsConstants.AppState, AppState.Active.ToString());

			// Verify the task is running
			if (IsMyBackgroundTaskRunning)
			{
				// If yes, it's safe to reconnect to media play handlers
				AddMediaPlayerEventHandlers();

				// Send message to background task that app is resumed so it can start sending notifications again
				MessageService.SendMessageToBackground(new AppResumedMessage());

				UpdateTransportControls(CurrentPlayer.CurrentState);

				var trackId = GetCurrentTrackIdAfterAppResume();
				//				txtCurrentTrack.Text = trackId == null ? string.Empty : playlistView.GetSongById(trackId).Title;
				//				txtCurrentState.Text = CurrentPlayer.CurrentState.ToString();
			}
			else
			{
				//				playButton.Content = ">";     // Change to play button
				//				txtCurrentTrack.Text = string.Empty;
				//				txtCurrentState.Text = "Background Task Not Running";
			}
		}

		/// <summary>
		/// Send message to Background process that app is to be suspended
		/// Stop clock and slider when suspending
		/// Unsubscribe handlers for MediaPlayer events
		/// </summary>
		void ForegroundApp_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
		{
			var deferral = e.SuspendingOperation.GetDeferral();

			// Only if the background task is already running would we do these, otherwise
			// it would trigger starting up the background task when trying to suspend.
			if (IsMyBackgroundTaskRunning)
			{
				// Stop handling player events immediately
				RemoveMediaPlayerEventHandlers();

				// Tell the background task the foreground is suspended
				MessageService.SendMessageToBackground(new AppSuspendedMessage());
			}
			
			// Persist that the foreground app is suspended
			ApplicationSettingsHelper.SaveSettingsValue(ApplicationSettingsConstants.AppState, AppState.Suspended.ToString());

			deferral.Complete();
		}
		#endregion





		

		/// <summary>
		/// The background task did exist, but it has disappeared. Put the foreground back into an initial state. Unfortunately,
		/// any attempts to unregister things on BackgroundMediaPlayer.Current will fail with the RPC error once the background task has been lost.
		/// </summary>
		private void ResetAfterLostBackground()
		{
			BackgroundMediaPlayer.Shutdown();
			_isMyBackgroundTaskRunning = false;
			backgroundAudioTaskStarted.Reset();
			//			prevButton.IsEnabled = true;
			//			nextButton.IsEnabled = true;
			ApplicationSettingsHelper.SaveSettingsValue(ApplicationSettingsConstants.BackgroundTaskState, BackgroundTaskState.Unknown.ToString());
			//			playButton.Content = "| |";

			try
			{
				BackgroundMediaPlayer.MessageReceivedFromBackground += BackgroundMediaPlayer_MessageReceivedFromBackground;
			}
			catch (Exception ex)
			{
				if (ex.HResult == RPC_S_SERVER_UNAVAILABLE)
				{
					throw new Exception("Failed to get a MediaPlayer instance.");
				}
				else
				{
					throw;
				}
			}
		}


		private async void BackgroundMediaPlayer_MessageReceivedFromBackground(object sender, MediaPlayerDataReceivedEventArgs e)
		{
			TrackChangedMessage trackChangedMessage;
			if (MessageService.TryParseMessage(e.Data, out trackChangedMessage))
			{
				var dispatcher = Windows.UI.Core.CoreWindow.GetForCurrentThread().Dispatcher;

				// When foreground app is active change track based on background message
				await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
				{
					// If playback stopped then clear the UI
					if (trackChangedMessage.VideoId == null)
					{
						//                        playlistView.SelectedIndex = -1;
						//                       albumArt.Source = null;
						//                      txtCurrentTrack.Text = string.Empty;
						//                       prevButton.IsEnabled = false;
						//                       nextButton.IsEnabled = false;
						return;
					}

					//                    var songIndex = playlistView.GetSongIndexById(trackChangedMessage.TrackId);
					//var song = playlistView.Songs[songIndex];

					// Update list UI
					//playlistView.SelectedIndex = songIndex;

					// Update the album art
					//albumArt.Source = albumArtCache[song.AlbumArtUri.ToString()];

					// Update song title
					//txtCurrentTrack.Text = song.Title;

					// Ensure track buttons are re-enabled since they are disabled when pressed
					//prevButton.IsEnabled = true;
					//nextButton.IsEnabled = true;
				});
				return;
			}

			BackgroundAudioTaskStartedMessage backgroundAudioTaskStartedMessage;
			if (MessageService.TryParseMessage(e.Data, out backgroundAudioTaskStartedMessage))
			{
				// StartBackgroundAudioTask is waiting for this signal to know when the task is up and running
				// and ready to receive messages
				Debug.WriteLine("BackgroundAudioTask started");
				backgroundAudioTaskStarted.Set();
				return;
			}
		}

		/// <summary>
		/// MediaPlayer state changed event handlers. 
		/// Note that we can subscribe to events even if Media Player is playing media in background
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		async void MediaPlayer_CurrentStateChanged(MediaPlayer sender, object args)
		{
			var currentState = sender.CurrentState; // cache outside of completion or you might get a different value
													//			await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
													//			{
													// Update state label
													//				txtCurrentState.Text = currentState.ToString();

			// Update controls
			//				UpdateTransportControls(currentState);
			//			});
		}


		/// <summary>
		/// Unsubscribes to MediaPlayer events. Should run only on suspend
		/// </summary>
		private void RemoveMediaPlayerEventHandlers()
		{
			try
			{
				BackgroundMediaPlayer.Current.CurrentStateChanged -= this.MediaPlayer_CurrentStateChanged;
				BackgroundMediaPlayer.MessageReceivedFromBackground -= BackgroundMediaPlayer_MessageReceivedFromBackground;
			}
			catch (Exception ex)
			{
				if (ex.HResult == RPC_S_SERVER_UNAVAILABLE)
				{
					// do nothing
				}
				else
				{
					throw;
				}
			}
		}

		/// <summary>
		/// Subscribes to MediaPlayer events
		/// </summary>
		private void AddMediaPlayerEventHandlers()
		{
			CurrentPlayer.CurrentStateChanged += this.MediaPlayer_CurrentStateChanged;

			try
			{
				BackgroundMediaPlayer.MessageReceivedFromBackground += BackgroundMediaPlayer_MessageReceivedFromBackground;
			}
			catch (Exception ex)
			{
				if (ex.HResult == RPC_S_SERVER_UNAVAILABLE)
				{
					// Internally MessageReceivedFromBackground calls Current which can throw RPC_S_SERVER_UNAVAILABLE
					ResetAfterLostBackground();
				}
				else
				{
					throw;
				}
			}
		}


		private void UpdatePlayingMedia(string videoId)
		{
			// Start the background task if it wasn't running
			if (!IsMyBackgroundTaskRunning || MediaPlayerState.Closed == CurrentPlayer.CurrentState)
			{
				// First update the persisted start track
				ApplicationSettingsHelper.SaveSettingsValue(ApplicationSettingsConstants.VideoId, videoId);
				ApplicationSettingsHelper.SaveSettingsValue(ApplicationSettingsConstants.Position, new TimeSpan().ToString());

				// Start task
				//                StartBackgroundAudioTask();

			}
			else
			{
				// Switch to the selected track
				//                MessageService.SendMessageToBackground(new TrackChangedMessage(VideoId));

			}

			if (MediaPlayerState.Paused == CurrentPlayer.CurrentState)
			{
				//                CurrentPlayer.Play();
			}
		}


		/// <summary>
		/// Initialize Background Media Player Handlers and starts playback
		/// </summary>
		private void StartBackgroundAudioTask()
		{
			if (IsMyBackgroundTaskRunning)
			{
				return;
			}

			AddMediaPlayerEventHandlers();




			var dispatcher = Windows.UI.Core.CoreWindow.GetForCurrentThread().Dispatcher;

			var startResult = dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				bool result = backgroundAudioTaskStarted.WaitOne(10000);
				//Send message to initiate playback
				if (result == true)
				{

				}
				else
				{
					throw new Exception("Background Audio Task didn't start in expected time");
				}
			});

			startResult.Completed = new AsyncActionCompletedHandler(BackgroundTaskInitializationCompleted);
		}

		private void BackgroundTaskInitializationCompleted(IAsyncAction action, AsyncStatus status)
		{
			if (status == AsyncStatus.Completed)
			{
				Debug.WriteLine("Background Audio Task initialized");
			}
			else if (status == AsyncStatus.Error)
			{
				Debug.WriteLine("Background Audio Task could not initialized due to an error ::" + action.ErrorCode.ToString());
			}
		}

		
	}
}
