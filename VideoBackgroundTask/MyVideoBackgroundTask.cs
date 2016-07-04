using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Media;
using Windows.Media.Playback;
using BackgroundAudioShared;
using BackgroundAudioShared.Messages;
using System.Threading;
using Windows.Foundation;
using Windows.Media.Core;

namespace VideoBackgroundTask
{
	public sealed class MyVideoBackgroundTask : IBackgroundTask
	{

		private const string TrackIdKey = "trackid";
		private const string TitleKey = "title";
		private const string AlbumArtKey = "albumart";

		private SystemMediaTransportControls smtc;
		private MediaPlaybackList playbackList = new MediaPlaybackList();
		private BackgroundTaskDeferral deferral; // Used to keep task alive
		private AppState foregroundAppState = AppState.Unknown;
		private ManualResetEvent backgroundTaskStarted = new ManualResetEvent(false);
		private bool playbackStartedPreviously;

		#region Helper methods
		Uri GetCurrentTrackId()
		{
			if (playbackList == null)
				return null;

			return GetTrackId(playbackList.CurrentItem);
		}

		Uri GetTrackId(MediaPlaybackItem item)
		{
			if (item == null)
				return null; // no track playing

			return item.Source.CustomProperties[TrackIdKey] as Uri;
		}
		#endregion

		public void Run(IBackgroundTaskInstance taskInstance)
		{
			Debug.WriteLine("Background Video Task " + taskInstance.Task.Name + " starting...");

			smtc = BackgroundMediaPlayer.Current.SystemMediaTransportControls;
			smtc.ButtonPressed += Smtc_ButtonPressed;
			smtc.PropertyChanged += Smtc_PropertyChanged;
			smtc.IsEnabled = true;
			smtc.IsPauseEnabled = true;
			smtc.IsPlayEnabled = true;
			smtc.IsRewindEnabled = true;

			BackgroundMediaPlayer.Current.CurrentStateChanged += Current_CurrentStateChanged;
			BackgroundMediaPlayer.MessageReceivedFromForeground += BackgroundMediaPlayer_MessageReceivedFromForeground;

			
			if (foregroundAppState != AppState.Suspended)
			{
				MessageService.SendMessageToForeground(new BackgroundAudioTaskStartedMessage());
			}

			ApplicationSettingsHelper.SaveSettingsValue(ApplicationSettingsConstants.BackgroundTaskState, BackgroundTaskState.Running.ToString());


			deferral = taskInstance.GetDeferral();

			backgroundTaskStarted.Set();

			taskInstance.Task.Completed += TaskCompleted;
			taskInstance.Canceled += new BackgroundTaskCanceledEventHandler(OnCanceled);
		}

		private void Smtc_PropertyChanged(SystemMediaTransportControls sender, SystemMediaTransportControlsPropertyChangedEventArgs args)
		{
			// If soundlevel turns to muted, app can choose to pause the music
		}

		private void Smtc_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
		{
			switch (args.Button)
			{
				case SystemMediaTransportControlsButton.Play:
					Debug.WriteLine("UVC play button pressed");

					// When the background task has been suspended and the SMTC
					// starts it again asynchronously, some time is needed to let
					// the task startup process in Run() complete.

					// Wait for task to start. 
					// Once started, this stays signaled until shutdown so it won't wait
					// again unless it needs to.
					bool result = backgroundTaskStarted.WaitOne(5000);
					if (!result)
						throw new Exception("Background Task didnt initialize in time");

					StartPlayback();
					break;
				case SystemMediaTransportControlsButton.Pause:
					Debug.WriteLine("UVC pause button pressed");
					try
					{
						BackgroundMediaPlayer.Current.Pause();
					}
					catch (Exception ex)
					{
						Debug.WriteLine(ex.ToString());
					}
					break;
				case SystemMediaTransportControlsButton.Next:
					Debug.WriteLine("UVC next button pressed");
//					SkipToNext();
					break;
				case SystemMediaTransportControlsButton.Previous:
					Debug.WriteLine("UVC previous button pressed");
//					SkipToPrevious();
					break;
			}
		}


		/// <summary>
		/// Indicate that the background task is completed.
		/// </summary>       
		void TaskCompleted(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
		{
			Debug.WriteLine("MyBackgroundAudioTask " + sender.TaskId + " Completed...");
			deferral.Complete();
		}


		/// <summary>
		/// Handles background task cancellation. Task cancellation happens due to:
		/// 1. Another Media app comes into foreground and starts playing music 
		/// 2. Resource pressure. Your task is consuming more CPU and memory than allowed.
		/// In either case, save state so that if foreground app resumes it can know where to start.
		/// </summary>
		private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
		{
			// You get some time here to save your state before process and resources are reclaimed
			Debug.WriteLine("MyBackgroundAudioTask " + sender.Task.TaskId + " Cancel Requested...");
			try
			{
				// immediately set not running
				backgroundTaskStarted.Reset();

				// save state
//				ApplicationSettingsHelper.SaveSettingsValue(ApplicationSettingsConstants.TrackId, GetCurrentTrackId() == null ? null : GetCurrentTrackId().ToString());
				ApplicationSettingsHelper.SaveSettingsValue(ApplicationSettingsConstants.Position, BackgroundMediaPlayer.Current.Position.ToString());
				ApplicationSettingsHelper.SaveSettingsValue(ApplicationSettingsConstants.BackgroundTaskState, BackgroundTaskState.Canceled.ToString());
				ApplicationSettingsHelper.SaveSettingsValue(ApplicationSettingsConstants.AppState, Enum.GetName(typeof(AppState), foregroundAppState));

				// unsubscribe from list changes
//				if (playbackList != null)
				{
//					playbackList.CurrentItemChanged -= PlaybackList_CurrentItemChanged;
//					playbackList = null;
				}

				// unsubscribe event handlers
				BackgroundMediaPlayer.MessageReceivedFromForeground -= BackgroundMediaPlayer_MessageReceivedFromForeground;
				smtc.ButtonPressed -= Smtc_ButtonPressed;
				smtc.PropertyChanged -= Smtc_PropertyChanged;
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.ToString());
			}
			deferral.Complete(); // signals task completion. 
			Debug.WriteLine("MyBackgroundAudioTask Cancel complete...");
		}

		/// <summary>
		/// Start playlist and change UVC state
		/// </summary>
		private void StartPlayback()
		{
			BackgroundMediaPlayer.Current.Play();
			/*
			try
			{
				// If playback was already started once we can just resume playing.
				if (!playbackStartedPreviously)
				{
					playbackStartedPreviously = true;

					// If the task was cancelled we would have saved the current track and its position. We will try playback from there.
					var currentTrackId = ApplicationSettingsHelper.ReadResetSettingsValue(ApplicationSettingsConstants.VideoId);
					var currentTrackPosition = ApplicationSettingsHelper.ReadResetSettingsValue(ApplicationSettingsConstants.Position);
					if (currentTrackId != null)
					{
						// Find the index of the item by name
						var index = playbackList.Items.ToList().FindIndex(item =>
							GetTrackId(item).ToString() == (string)currentTrackId);

						if (currentTrackPosition == null)
						{
							// Play from start if we dont have position
							Debug.WriteLine("StartPlayback: Switching to track " + index);
//							playbackList.MoveTo((uint)index);

							// Begin playing
							BackgroundMediaPlayer.Current.Play();
						}
						else
						{
							// Play from exact position otherwise
							TypedEventHandler<MediaPlaybackList, CurrentMediaPlaybackItemChangedEventArgs> handler = null;
							handler = (MediaPlaybackList list, CurrentMediaPlaybackItemChangedEventArgs args) =>
							{
								if (args.NewItem == playbackList.Items[index])
								{
									// Unsubscribe because this only had to run once for this item
									playbackList.CurrentItemChanged -= handler;

									// Set position
									var position = TimeSpan.Parse((string)currentTrackPosition);
									Debug.WriteLine("StartPlayback: Setting Position " + position);
									BackgroundMediaPlayer.Current.Position = position;

									// Begin playing
									BackgroundMediaPlayer.Current.Play();
								}
							};
							playbackList.CurrentItemChanged += handler;

							// Switch to the track which will trigger an item changed event
							Debug.WriteLine("StartPlayback: Switching to track " + index);
							playbackList.MoveTo((uint)index);
						}
					}
					else
					{
						// Begin playing
						BackgroundMediaPlayer.Current.Play();
					}
				}
				else
				{
					// Begin playing
					BackgroundMediaPlayer.Current.Play();
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.ToString());
			}
			*/
		}


		private void BackgroundMediaPlayer_MessageReceivedFromForeground(object sender, MediaPlayerDataReceivedEventArgs e)
		{
			AppSuspendedMessage appSuspendedMessage;
			if (MessageService.TryParseMessage(e.Data, out appSuspendedMessage))
			{
				Debug.WriteLine("App suspending"); // App is suspended, you can save your task state at this point
				foregroundAppState = AppState.Suspended;
//				var currentTrackId = GetCurrentTrackId();
//				ApplicationSettingsHelper.SaveSettingsValue(ApplicationSettingsConstants.TrackId, currentTrackId == null ? null : currentTrackId.ToString());
				return;
			}

			AppResumedMessage appResumedMessage;
			if (MessageService.TryParseMessage(e.Data, out appResumedMessage))
			{
				Debug.WriteLine("App resuming"); // App is resumed, now subscribe to message channel
				foregroundAppState = AppState.Active;
				return;
			}

			StartPlaybackMessage startPlaybackMessage;
			if (MessageService.TryParseMessage(e.Data, out startPlaybackMessage))
			{
				//Foreground App process has signalled that it is ready for playback
				Debug.WriteLine("Starting Playback");
				StartPlayback();
				return;
			}

			SkipNextMessage skipNextMessage;
			if (MessageService.TryParseMessage(e.Data, out skipNextMessage))
			{
				// User has chosen to skip track from app context.
				Debug.WriteLine("Skipping to next");
//				SkipToNext();
				return;
			}

			SkipPreviousMessage skipPreviousMessage;
			if (MessageService.TryParseMessage(e.Data, out skipPreviousMessage))
			{
				// User has chosen to skip track from app context.
				Debug.WriteLine("Skipping to previous");
				//SkipToPrevious();
				return;
			}

			TrackChangedMessage trackChangedMessage;
			if (MessageService.TryParseMessage(e.Data, out trackChangedMessage))
			{
//				var index = playbackList.Items.ToList().FindIndex(i => (Uri)i.Source.CustomProperties[TrackIdKey] == trackChangedMessage.TrackId);
//				Debug.WriteLine("Skipping to track " + index);
				smtc.PlaybackStatus = MediaPlaybackStatus.Changing;
//				playbackList.MoveTo((uint)index);
				return;
			}

			UpdatePlaylistMessage updatePlaylistMessage;
			if (MessageService.TryParseMessage(e.Data, out updatePlaylistMessage))
			{
				//CreatePlaybackList(updatePlaylistMessage.Videos);
				return;
			}
		}

		private void Current_CurrentStateChanged(MediaPlayer sender, object args)
		{
			if (sender.CurrentState == MediaPlayerState.Playing)
			{
				smtc.PlaybackStatus = MediaPlaybackStatus.Playing;
			}
			else if (sender.CurrentState == MediaPlayerState.Paused)
			{
				smtc.PlaybackStatus = MediaPlaybackStatus.Paused;
			}
			else if (sender.CurrentState == MediaPlayerState.Closed)
			{
				smtc.PlaybackStatus = MediaPlaybackStatus.Closed;
			}
		}


		/// <summary>
		/// Create a playback list from the list of songs received from the foreground app.
		/// </summary>
		/// <param name="songs"></param>
		/// 
		/*
		void CreatePlaybackList(IEnumerable<NicoVideoModel> videos)
		{
			// Make a new list and enable looping
			playbackList = new MediaPlaybackList();
			playbackList.AutoRepeatEnabled = true;

			// Add playback items to the list
			foreach (var song in songs)
			{
				var source = MediaSource.CreateFromUri(song.MediaUri);
				source.CustomProperties[TrackIdKey] = song.MediaUri;
				source.CustomProperties[TitleKey] = song.Title;
				source.CustomProperties[AlbumArtKey] = song.AlbumArtUri;
				playbackList.Items.Add(new MediaPlaybackItem(source));
			}

			// Don't auto start
			BackgroundMediaPlayer.Current.AutoPlay = false;

			// Assign the list to the player
			BackgroundMediaPlayer.Current.Source = playbackList;

			// Add handler for future playlist item changes
			playbackList.CurrentItemChanged += PlaybackList_CurrentItemChanged;
		}
		*/
	}

	public enum AppState
	{
	    Unknown,
	    Active,
	    Suspended
    }
}
