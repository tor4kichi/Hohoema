
using Hohoema.Models.Niconico.Video;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.ViewModels.Niconico.Video.Commands
{
    public abstract class VideoContentSelectionCommandBase : CommandBase
    {
        public VideoContentSelectionCommandBase()
        {
        }

        protected override bool CanExecute(object parameter)
        {
            return parameter is IVideoContent
                || parameter is IEnumerable<IVideoContent>
                ;
        }

        protected override void Execute(object parameter)
        {
            if (parameter is IVideoContent content)
            {
                Execute(content);
            }
            else if (parameter is IEnumerable<IVideoContent> items)
            {
                Execute(items.ToArray());
            }
        }

        protected virtual void Execute(IVideoContent content)
        {

        }

        protected virtual void Execute(IEnumerable<IVideoContent> items)
        {
            foreach (var item in items)
            {
                Execute(item);
            }
        }
    }
}
