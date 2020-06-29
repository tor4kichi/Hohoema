using Hohoema.Models.Repository;
using Hohoema.UseCase.VideoCache;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.ViewModels.VideoCache.Commands
{
    public sealed class DeleteCacheRequestCommand : DelegateCommandBase
    {
        private readonly VideoCacheManager _videoCacheManager;

        public DeleteCacheRequestCommand(VideoCacheManager videoCacheManager)
        {
            _videoCacheManager = videoCacheManager;
        }

        protected override bool CanExecute(object parameter)
        {
            return parameter is IVideoContent;
        }

        protected override void Execute(object parameter)
        {
            if (parameter is IVideoContent video)
            {
                _videoCacheManager.RequestCache(video.Id);
            }
        }
    }
}
