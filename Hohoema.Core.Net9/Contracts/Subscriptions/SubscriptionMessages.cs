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

public sealed class SubscriptionDeletedMessage : ValueChangedMessage<SubscriptionId>
{
    public SubscriptionDeletedMessage(SubscriptionId value) : base(value)
    {
    }
}

public sealed class SubscriptionUpdatedMessage : ValueChangedMessage<Subscription>
{
    public SubscriptionUpdatedMessage(Subscription value) : base(value)
    {
    }
}


public sealed class SubscriptionGroupMovedMessage : ValueChangedMessage<Subscription>
{
    public SubscriptionGroupMovedMessage(Subscription value) : base(value)
    {
    }

    public SubscriptionGroupId LastGroupId { get; init; }
    public SubscriptionGroupId CurrentGroupId { get; init; }
}

public sealed class SubscriptionCheckedAtChangedMessage : ValueChangedMessage<SubscriptionUpdate>
{
    public SubscriptionCheckedAtChangedMessage(SubscriptionUpdate value, SubscriptionGroupId subscriptionGroupId) : base(value)
    {
        SubscriptionGroupId = subscriptionGroupId;
    }

    public SubscriptionGroupId SubscriptionGroupId { get; }
}