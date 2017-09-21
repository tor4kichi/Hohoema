using NicoPlayerHohoema.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace NicoPlayerHohoema.Views.Converters
{
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
