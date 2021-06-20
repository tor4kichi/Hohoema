using Hohoema.Models.Domain.PageNavigation;
using LiteDB;
using Prism.Commands;
using Prism.Mvvm;
using System.Diagnostics;
using System.Windows.Input;
using Unity;

namespace Hohoema.Models.Domain.Pins
{
    public sealed class HohoemaPin : BindableBase
    {
        [BsonId(autoId:true)]
        public int Id { get; set; }

        [BsonField]
        public HohoemaPageType PageType { get; set; }
        [BsonField]
        public string Parameter { get; set; }
        [BsonField]
        public string Label { get; set; }

        private string _OverrideLabel;
        [BsonField]
        public string OverrideLabel
        {
            get { return _OverrideLabel; }
            set { SetProperty(ref _OverrideLabel, value); }
        }


        [BsonField]
        public int SortIndex { get; set; }
    }
}
