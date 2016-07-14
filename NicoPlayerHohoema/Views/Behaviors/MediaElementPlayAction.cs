using Microsoft.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace NicoPlayerHohoema.Views.Behaviors
{

	public class MediaElementPlayAction : Behavior<DependencyObject>, IAction
	{
		public static readonly DependencyProperty TargetObjectProperty =
			DependencyProperty.Register("TargetObject"
					, typeof(MediaElement)
					, typeof(MediaElementPlayAction)
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
				mediaElem.Play();
			}
			return true;
		}
	}
}
