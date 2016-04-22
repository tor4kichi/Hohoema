using System;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;

namespace NicoPlayerHohoema.Views.UIEffects
{
	public sealed partial class GlowEffectHostControl : ContentControl
	{
		public GlowEffectHostControl()
		{
			this.InitializeComponent();

			this.Loaded += OnLoaded;
			this.Unloaded += OnUnloaded;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			if (TargetUI != null)
			{
				var canvasControl = new CanvasControl();

				canvasControl.CreateResources += BGCanvas_CreateResources;
				canvasControl.Draw += BGCanvas_Draw;

				Canvas.SetZIndex(canvasControl, -1);

				var rootPanel = GetTemplateChild("RootPanel") as Panel;
				rootPanel.Children.Add(canvasControl);
			}
		}

		private void OnUnloaded(object sender, RoutedEventArgs e)
		{
			_shadowEffect?.Dispose();
			_bitmap?.Dispose();
		}

		/// <summary>
		/// 影Bitmapの作成
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		void BGCanvas_CreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs args)
		{
			args.TrackAsyncAction(sender.CreateBitmapFromUIElement(TargetUI)
				.ContinueWith(x =>
				{
					_bitmap = x.Result;

					_shadowEffect = new ShadowEffect
					{
						Source = _bitmap
					};
				}).AsAsyncAction());
		}


		/// <summary>
		/// ContentPresenter読み込み後に動的に追加したCanvasControlへの描画
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		void BGCanvas_Draw(CanvasControl sender, CanvasDrawEventArgs args)
		{
			_shadowEffect.BlurAmount = (float)GlowEffectBlurAmount;
			_shadowEffect.ShadowColor = GlowEffectColor;

			using (var session = args.DrawingSession)
			{
				session.DrawImage(_shadowEffect
					// Canvas上に描画する範囲
					, new Rect(GlowEffectTranslate.X, GlowEffectTranslate.Y, sender.ActualWidth, sender.ActualHeight)

					// Bitmapの参照範囲
					, new Rect(0, 0, _bitmap.SizeInPixels.Width, _bitmap.SizeInPixels.Height)

					, (float)GlowEffectOpacity
					);
			}
		}


		#region field value

		CanvasBitmap _bitmap;

		ShadowEffect _shadowEffect;

		#endregion


		#region GlowEffectTargetName Property

		public static readonly DependencyProperty GlowEffectTargetNameProperty =
			DependencyProperty.Register("GlowEffectTargetName"
				, typeof(string)
				, typeof(GlowEffectHostControl)
				, new PropertyMetadata("")
				);


		public string GlowEffectTargetName
		{
			get { return (string)GetValue(GlowEffectTargetNameProperty); }
			set { SetValue(GlowEffectTargetNameProperty, value); }
		}

		#endregion

		#region GlowEffectColor Property

		public static readonly DependencyProperty GlowEffectColorProperty =
			DependencyProperty.Register("GlowEffectColor"
				, typeof(Color)
				, typeof(GlowEffectHostControl)
				, new PropertyMetadata(Windows.UI.Colors.Black)
				);


		public Color GlowEffectColor
		{
			get { return (Color)GetValue(GlowEffectColorProperty); }
			set { SetValue(GlowEffectColorProperty, value); }
		}

		#endregion

		#region GlowEffectOpacity Property

		public static readonly DependencyProperty GlowEffectOpacityProperty =
			DependencyProperty.Register("GlowEffectOpacity"
				, typeof(double)
				, typeof(GlowEffectHostControl)
				, new PropertyMetadata(1.0)
				);


		public double GlowEffectOpacity
		{
			get { return (double)GetValue(GlowEffectOpacityProperty); }
			set { SetValue(GlowEffectOpacityProperty, value); }
		}

		#endregion

		#region GlowEffectBlurAmount Property

		public static readonly DependencyProperty GlowEffectBlurAmountProperty =
			DependencyProperty.Register("GlowEffectBlurAmount"
				, typeof(double)
				, typeof(GlowEffectHostControl)
				, new PropertyMetadata(1.0)
				);


		public double GlowEffectBlurAmount
		{
			get { return (double)GetValue(GlowEffectBlurAmountProperty); }
			set { SetValue(GlowEffectBlurAmountProperty, value); }
		}

		#endregion

		#region GlowEffectTranslate Property

		public static readonly DependencyProperty GlowEffectTranslateProperty =
			DependencyProperty.Register("GlowEffectTranslate"
				, typeof(TranslateTransform)
				, typeof(GlowEffectHostControl)
				, new PropertyMetadata(new TranslateTransform())
				);


		public TranslateTransform GlowEffectTranslate
		{
			get { return (TranslateTransform)GetValue(GlowEffectTranslateProperty); }
			set { SetValue(GlowEffectTranslateProperty, value); }
		}

		#endregion



		private UIElement TargetUI
		{
			get
			{
				return GetTemplateChild("ShadowTargetContentPresenter") as UIElement;
			}
		}
	}
}
