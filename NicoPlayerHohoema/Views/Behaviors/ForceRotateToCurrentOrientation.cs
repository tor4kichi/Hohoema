using Microsoft.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Sensors;
using Windows.Graphics.Display;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace NicoPlayerHohoema.Views.Behaviors
{
	public class ForceRotateToCurrentOrientation : DependencyObject, IAction
	{

		private double DefaultHeight = 0.0;
		private double DefaultWidth = 0.0;


		#region IsEnable Property

		public static readonly DependencyProperty TargetProperty =
			DependencyProperty.Register("Target"
					, typeof(FrameworkElement)
					, typeof(ForceRotateToCurrentOrientation)
					, new PropertyMetadata(default(FrameworkElement), OnTargetPropertyChanged)
				);

		public FrameworkElement Target
		{
			get { return (FrameworkElement)GetValue(TargetProperty); }
			set { SetValue(TargetProperty, value); }
		}

		public static void OnTargetPropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
		{
			ForceRotateToCurrentOrientation source = (ForceRotateToCurrentOrientation)sender;

			var displayInfo = Windows.Graphics.Display.DisplayInformation.GetForCurrentView();
			
		}

		private void DisplayInfo_OrientationChanged(DisplayInformation sender, object args)
		{
			var newTarget = Target;
		}


		#endregion

		//C#
		[DllImport("user32.dll", EntryPoint = "#2507")]
		extern static bool SetAutoRotation(bool bEnable);

		public enum tagAR_STATE : uint
		{
			AR_ENABLED = 0x0,
			AR_DISABLED = 0x1,
			AR_SUPPRESSED = 0x2,
			AR_REMOTESESSION = 0x4,
			AR_MULTIMON = 0x8,
			AR_NOSENSOR = 0x10,
			AR_NOT_SUPPORTED = 0x20,
			AR_DOCKED = 0x40,
			AR_LAPTOP = 0x80
		}

		[DllImport("user32.dll")]
		public static extern bool GetAutoRotationState(ref tagAR_STATE input);


		public object Execute(object sender, object parameter)
		{
			var target = Target;

			if (target == null) { return false; }

			var simple = SimpleOrientationSensor.GetDefault();
			var displayInfo = Windows.Graphics.Display.DisplayInformation.GetForCurrentView();

			var isNativeOrientationPortrait = displayInfo.NativeOrientation == DisplayOrientations.Portrait;
			DisplayOrientations orientation = DisplayOrientations.None;
			var realOrientation = simple.GetCurrentOrientation();
			switch (realOrientation)
			{
				case SimpleOrientation.NotRotated:
					orientation = isNativeOrientationPortrait ? DisplayOrientations.Portrait : DisplayOrientations.Landscape;
					break;
				case SimpleOrientation.Rotated90DegreesCounterclockwise:
					orientation = isNativeOrientationPortrait ? DisplayOrientations.LandscapeFlipped : DisplayOrientations.Portrait;
					break;
				case SimpleOrientation.Rotated180DegreesCounterclockwise:
					orientation = isNativeOrientationPortrait ? DisplayOrientations.PortraitFlipped : DisplayOrientations.LandscapeFlipped;
					break;
				case SimpleOrientation.Rotated270DegreesCounterclockwise:
					orientation = isNativeOrientationPortrait ? DisplayOrientations.Landscape : DisplayOrientations.Portrait;
					break;
			}

			tagAR_STATE state = default(tagAR_STATE);
			GetAutoRotationState(ref state);

			SetAutoRotation(true);

			Windows.Graphics.Display.DisplayInformation.AutoRotationPreferences = orientation;

			if (state != tagAR_STATE.AR_ENABLED)
			{
				SetAutoRotation(false);
			}

			Target.UpdateLayout();

			return true;
		}

		private int DisplayOrientationToIndex(DisplayOrientations orientation)
		{
			switch (orientation)
			{
				case DisplayOrientations.None:
					return 1;
				case DisplayOrientations.Landscape:
					return 1;
				case DisplayOrientations.Portrait:
					return 2;
				case DisplayOrientations.LandscapeFlipped:
					return 3;
				case DisplayOrientations.PortraitFlipped:
					return 4;
				default:
					return 0;
			}
		}

		private int DisplayOrientationToIndex(SimpleOrientation orientation, DisplayOrientations nativeOrientaion)
		{
			bool isPortraitNative = nativeOrientaion == DisplayOrientations.Portrait;

			if (isPortraitNative)
			{
				switch (orientation)
				{
					case SimpleOrientation.NotRotated:
						return 2;
					case SimpleOrientation.Rotated90DegreesCounterclockwise:
						return 1;
					case SimpleOrientation.Rotated180DegreesCounterclockwise:
						return 4;
					case SimpleOrientation.Rotated270DegreesCounterclockwise:
						return 3;
					default:
						return 0;
				}
			}
			else
			{
				switch (orientation)
				{
					case SimpleOrientation.NotRotated:
						return 1;
					case SimpleOrientation.Rotated90DegreesCounterclockwise:
						return 2;
					case SimpleOrientation.Rotated180DegreesCounterclockwise:
						return 3;
					case SimpleOrientation.Rotated270DegreesCounterclockwise:
						return 4;
					default:
						return 0;
				}
			}
		}
	}
}
