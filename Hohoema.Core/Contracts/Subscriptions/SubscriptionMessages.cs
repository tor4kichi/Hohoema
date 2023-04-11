using CommunityToolkit.Mvvm.Messaging.Messages;
using Hohoema.Models.Subscriptions;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Contracts.Subscriptions;

public sealed class SubscriptionAddedMessage : ValueChangedMessage<Subscription>
{
    public SubscriptionAddedMessage(Subscription value) : base(value)
    {
    }
}

public sealed class SubscriptionDeletedMessage : ValueChangedMessage<ObjectId>
{
    public SubscriptionDeletedMessage(ObjectId value) : base(value)
    {
    }
}

public sealed class SubscriptionUpdatedMessage : ValueChangedMessage<Subscription>
{
    public SubscriptionUpdatedMessage(Subscription value) : base(value)
    {
    }
}