using Hohoema.Database;
using Hohoema.Models.Repository.Niconico;
using System;
using System.Collections.Generic;

namespace Hohoema.Models.Repository.Niconico.Mylist
{
    public interface IMylistGroupResult
    {
        bool IsOK { get; }
        DateTime CreateTime { get; }
        MylistGroupDefaultSort DefaultSort { get; }
        string Description { get; }
        MylistGroupIconType IconType { get; }
        Order Order {get;}
        string Id { get; }
        bool IsPublic { get; }
        int ItemsCount { get; }
        string Name { get; }
        IList<Database.NicoVideo> SampleVideos { get; }
        DateTime UpdateTime { get; }
        string UserId { get; }
        int ViewCount { get; }
    }
}