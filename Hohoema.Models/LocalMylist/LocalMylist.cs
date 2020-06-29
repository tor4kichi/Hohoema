using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.LocalMylist
{
    public enum ContentInsertPosition
    {
        Head,
        Tail,
    }

    public sealed class LocalMylistGroup 
    {
        public LocalMylistGroup() { }

        public LocalMylistGroup(string id, string label)
            : base()
        {
            Id = id;
            Label = label;
        }

        public string Id { get; internal set; }

        public int SortIndex { get; internal set; }

        string _Label;
        public string Label
        {
            get { return _Label; }
            set
            {
                if (_Label != value)
                {
                    _Label = value;
//                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(Label)));
                }
            }
        }

        public int Count { get; }
    }
}
