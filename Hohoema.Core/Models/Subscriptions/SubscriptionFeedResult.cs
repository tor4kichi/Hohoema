﻿using LiteDB;
using System;
using System.Collections.Generic;

namespace Hohoema.Models.Subscriptions;

public class FeedResultVideoItem
{
    public string VideoId { get; set; }
    public DateTime PostAt { get; set; }
    public string Title { get; set; }
}

public sealed class SubscriptionFeedResult
{
    [BsonId]
    public ObjectId Id { get; internal set; }

    public string SourceParamater { get; set; }

    public SubscriptionSourceType SourceType { get; set; }

    public DateTime LastUpdatedAt { get; set; }

    public IList<FeedResultVideoItem> Videos { get; set; }
}
