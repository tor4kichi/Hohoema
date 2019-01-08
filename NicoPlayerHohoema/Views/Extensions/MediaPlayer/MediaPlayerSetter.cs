using Microsoft.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Playback;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace NicoPlayerHohoema.Views.Behaviors
{
    public sealed class MediaPlayerSetter : Behavior<MediaPlayerElement>
    {
        #region Position Property

        public static readonly DependencyProperty MediaPlayerProperty =
            DependencyProperty.Register("MediaPlayer"
                    , typeof(MediaPlayer)
                    , typeof(MediaPlayerSetter)
                    , new PropertyMetadata(default(MediaPlayer), OnMediaPlayerPropertyChanged)
                );

        public MediaPlayer MediaPlayer
        {
            get { return (MediaPlayer)GetValue(MediaPlayerProperty); }
            set { SetValue(MediaPlayerProperty, value); }
        }


        public static void OnMediaPlayerPropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            MediaPlayerSetter source = (MediaPlayerSetter)sender;
            if (source.MediaPlayer != null)
            {
                source.AssociatedObject.SetMediaPlayer(source.MediaPlayer);
            }
        }

        #endregion




        protected override void OnAttached()
        {
            base.OnAttached();

            this.AssociatedObject.Loaded += AssociatedObject_Loaded;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.SetMediaPlayer(null);

            base.OnDetaching();
        }

        private void AssociatedObject_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (MediaPlayer != null)
            {
                AssociatedObject.SetMediaPlayer(MediaPlayer);
            }
        }
    }
}
