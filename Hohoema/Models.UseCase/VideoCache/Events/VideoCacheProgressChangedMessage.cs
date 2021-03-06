﻿using Hohoema.Models.Domain.VideoCache;
using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.UseCase.VideoCache.Events
{
    public sealed class VideoCacheProgressChangedMessage : ValueChangedMessage<VideoCacheItem>
    {
        public VideoCacheProgressChangedMessage(VideoCacheItem value) : base(value)
        {
        }
    }
}
