using CommunityToolkit.Mvvm.Messaging.Messages;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Subscriptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Contracts.Subscriptions;
public sealed class NewSubscFeedVideoMessage : ValueChangedMessage<SubscFeedVideo>
{
    public NewSubscFeedVideoMessage(SubscFeedVideo value) : base(value)
    {

    }
}



public sealed class SubscFeedVideoValueChangedMessage : ValueChangedMessage<SubscFeedVideo>
{
    public SubscFeedVideoValueChangedMessage(SubscFeedVideo value) : base(value)
    {

    }
}