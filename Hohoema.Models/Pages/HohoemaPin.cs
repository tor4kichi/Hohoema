using System.Diagnostics;
using System.Windows.Input;
using Hohoema.Services.Helpers;
using LiteDB;

namespace Hohoema.Models.Pages
{
    public sealed class HohoemaPin : FixPrism.BindableBase
    {
        [BsonId(autoId: true)]
        public int Id { get; set; }

        [BsonField]
        public int SortIndex { get; }
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
    }
}
