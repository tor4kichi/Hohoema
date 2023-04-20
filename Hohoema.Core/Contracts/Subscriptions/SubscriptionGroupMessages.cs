using CommunityToolkit.Mvvm.Messaging.Messages;
using Hohoema.Models.Subscriptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Contracts.Subscriptions;

public sealed class SubscriptionGroupCreatedMessage : ValueChangedMessage<SubscriptionGroup>
{
    public SubscriptionGroupCreatedMessage(SubscriptionGroup value) : base(value)
    {
    }
}

public sealed class SubscriptionGroupDeletedMessage : ValueChangedMessage<SubscriptionGroup>
{
    public SubscriptionGroupDeletedMessage(SubscriptionGroup value) : base(value)
    {
    }
}

public sealed class SubscriptionGroupReorderedMessage : ValueChangedMessage<IReadOnlyCollection<SubscriptionGroup>>
{
    public SubscriptionGroupReorderedMessage(IReadOnlyCollection<SubscriptionGroup> value) : base(value)
    {
    }
}


public sealed class SubscriptionGroupCheckedAtChangedMessage : ValueChangedMessage<DateTime>
{
    public SubscriptionGroupCheckedAtChangedMessage(SubscriptionGroupId subscriptionGroupId, DateTime checkedAt) : base(checkedAt)
    {
        SubscriptionGroupId = subscriptionGroupId;        
    }

    public SubscriptionGroupId SubscriptionGroupId { get; }
}

public sealed class SubscriptionGroupPropsChangedMessage : ValueChangedMessage<SubscriptionGroupProps>
{
    public SubscriptionGroupPropsChangedMessage(SubscriptionGroupProps value) : base(value)
    {
    }
}