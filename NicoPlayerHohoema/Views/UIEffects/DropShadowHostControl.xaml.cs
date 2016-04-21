using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace NicoPlayerHohoema.Views.UIEffects
{
	public sealed partial class DropShadowHostControl : UserControl
	{
		#region Target Property

		public static readonly DependencyProperty TargetProperty =
			DependencyProperty.Register("Target"
				, typeof(string)
				, typeof(DropShadowHostControl)
				, new PropertyMetadata("")
				);


		public string Target
		{
			get { return (string)GetValue(TargetProperty); }
			set { SetValue(TargetProperty, value); }
		}

		#endregion

		#region ShadowColor Property

		public static readonly DependencyProperty ShadowColorProperty =
			DependencyProperty.Register("ShadowColor"
				, typeof(Color)
				, typeof(DropShadowHostControl)
				, new PropertyMetadata(Windows.UI.Colors.Black)
				);


		public Color ShadowColor
		{
			get { return (Color)GetValue(ShadowColorProperty); }
			set { SetValue(ShadowColorProperty, value); }
		}

		#endregion

		#region ShadowOpacity Property

		public static readonly DependencyProperty ShadowOpacityProperty =
			DependencyProperty.Register("ShadowOpacity"
				, typeof(float)
				, typeof(DropShadowHostControl)
				, new PropertyMetadata(1.0f)
				);


		public float ShadowOpacity
		{
			get { return (float)GetValue(ShadowOpacityProperty); }
			set { SetValue(ShadowOpacityProperty, value); }
		}

		#endregion

		#region ShadowBlurAmount Property

		public static readonly DependencyProperty ShadowBlurAmountProperty =
			DependencyProperty.Register("ShadowBlurAmount"
				, typeof(double)
				, typeof(DropShadowHostControl)
				, new PropertyMetadata(1.0)
				);


		public double ShadowBlurAmount
		{
			get { return (double)GetValue(ShadowBlurAmountProperty); }
			set { SetValue(ShadowBlurAmountProperty, value); }
		}

		#endregion

		#region ShadowTranslate Property

		public static readonly DependencyProperty ShadowTranslateProperty =
			DependencyProperty.Register("ShadowTranslate"
				, typeof(TranslateTransform)
				, typeof(DropShadowHostControl)
				, new PropertyMetadata(new TranslateTransform())
				);


		public TranslateTransform ShadowTranslate
		{
			get { return (TranslateTransform)GetValue(ShadowTranslateProperty); }
			set { SetValue(ShadowTranslateProperty, value); }
		}

		#endregion


		CanvasBitmap _bitmap;

		ShadowEffect _shadowEffect;



		private UIElement TargetUI
		{
			get
			{
				return (this.Parent as FrameworkElement).FindName(Target) as UIElement;
			}
		}

		public DropShadowHostControl()
		{
			this.InitializeComponent();

			this.Loaded += OnLoaded;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			if (TargetUI != null)
			{
				var canvasControl = new CanvasControl();
				var rootPanel = this.GetTemplateChild("RootCanvas") as Panel;

				Canvas.SetZIndex(canvasControl, -1);

				canvasControl.CreateResources += BGCanvas_CreateResources;
				canvasControl.Draw += BGCanvas_Draw;
				this.Content = canvasControl;

				this.Unloaded += DropShadowHostControl_Unloaded;
			}

		}



		private void OnParentLoaded(object sender, RoutedEventArgs e)
		{			
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();


		}

		private void DropShadowHostControl_Unloaded(object sender, RoutedEventArgs e)
		{
			_shadowEffect.Dispose();
			_bitmap.Dispose();
		}

		void BGCanvas_CreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs args)
		{
			args.TrackAsyncAction(sender.CreateUIElementBitmap(TargetUI)
				.ContinueWith(x =>
				{
					_bitmap = x.Result;

					_shadowEffect = new ShadowEffect
					{
						Source = _bitmap
					};
				}).AsAsyncAction());

			// TODO: CommandListで書き直す？
		}


		void BGCanvas_Draw(CanvasControl sender, CanvasDrawEventArgs args)
		{
			if (_bitmap == null)
			{
				return;
			}

			_shadowEffect.BlurAmount = (float)ShadowBlurAmount;
			_shadowEffect.ShadowColor = ShadowColor;

			using (var session = args.DrawingSession)
			{
				session.DrawImage(_shadowEffect
					// Canvas上に描画する範囲
					, new Rect(ShadowTranslate.X, ShadowTranslate.Y, sender.ActualWidth, sender.ActualHeight)

					// Bitmapの参照範囲
					, new Rect(0, 0, _bitmap.SizeInPixels.Width, _bitmap.SizeInPixels.Height)
					
					, ShadowOpacity
					);
			}
		}
	}
}
