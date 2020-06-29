using Hohoema.Models.Repository.Niconico.Mylist;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.Niconico
{

    public sealed class MylistGroupData
    {
        private readonly Mntone.Nico2.Mylist.MylistGroupData _groupData;

        internal MylistGroupData(Mntone.Nico2.Mylist.MylistGroupData groupData)
        {
            _groupData = groupData;
        }

        public string Id => _groupData.Id;

        public string UserId => _groupData.UserId;

        public string Name => _groupData.Name;

        public string Description => _groupData.Description;

        public bool IsPublic => _groupData.GetIsPublic();

        public int ItemsCount => _groupData.Count;

        public MylistGroupIconType IconType => _groupData.GetIconType().ToModelIconType();
    }

    public sealed class UserMylistGroupData
    {
        
    }
}
