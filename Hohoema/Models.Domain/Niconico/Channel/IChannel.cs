﻿using Hohoema.Models.Domain.Niconico.Follow;
using Hohoema.Models.Domain.Niconico.Follow.LoginUser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Niconico.Channel
{
    public interface IChannel : INiconicoGroup, IFollowable
    {
    }
}
