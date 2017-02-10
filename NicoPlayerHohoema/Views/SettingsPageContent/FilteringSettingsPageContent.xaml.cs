using NicoPlayerHohoema.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace NicoPlayerHohoema.Views.SettingsPageContent
{
	public sealed partial class FilteringSettingPageContent : UserControl
	{
		public FilteringSettingPageContent()
		{
			this.InitializeComponent();
		}
	}




    public class NGCommentScoreTypeTemplateSelector : DataTemplateSelector
    {
        public DataTemplate NoneTemplate { get; set; }
        public DataTemplate LowTemplate { get; set; }
        public DataTemplate MiddleTemplate { get; set; }
        public DataTemplate HighTemplate { get; set; }
        public DataTemplate VeryHighTemplate { get; set; }
        public DataTemplate SuperVeryHighTemplate { get; set; }
        public DataTemplate UltraSuperVeryHighTemplate { get; set; }


        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            NGCommentScore scoreType = (NGCommentScore)item;

            switch (scoreType)
            {
                case NGCommentScore.None:
                    return NoneTemplate;
                case NGCommentScore.Low:
                    return LowTemplate;
                case NGCommentScore.Middle:
                    return MiddleTemplate;
                case NGCommentScore.High:
                    return HighTemplate;
                case NGCommentScore.VeryHigh:
                    return VeryHighTemplate;
                case NGCommentScore.SuperVeryHigh:
                    return SuperVeryHighTemplate;
                case NGCommentScore.UltraSuperVeryHigh:
                    return UltraSuperVeryHighTemplate;
                default:
                    throw new NotSupportedException($"not suppoert {nameof(NGCommentScore)}.{scoreType.ToString()}");

            }
        }
    }

}
