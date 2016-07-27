using Microsoft.Xaml.Interactivity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;


namespace NicoPlayerHohoema.Views.Behaviors
{
	public class MediaElementTogglePlayback : Behavior<DependencyObject>, IAction
	{
		public static readonly DependencyProperty TargetObjectProperty =
			DependencyProperty.Register("TargetObject"
					, typeof(MediaElement)
					, typeof(MediaElementTogglePlayback)
					, new PropertyMetadata(null)
				);

		public MediaElement TargetObject
		{
			get { return (MediaElement)GetValue(TargetObjectProperty); }
			set { SetValue(TargetObjectProperty, value); }
		}

		public object Execute(object sender, object parameter)
		{
			var mediaElem = TargetObject;
			if (mediaElem != null)
			{
				if (mediaElem.CurrentState == Windows.UI.Xaml.Media.MediaElementState.Paused)
				{
					mediaElem.Play();
				}
				else if (mediaElem.CurrentState == Windows.UI.Xaml.Media.MediaElementState.Playing)
				{
					mediaElem.Pause();
				}
			}

			return true;
		}
	}
}
