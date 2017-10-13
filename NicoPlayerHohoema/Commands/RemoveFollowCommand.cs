using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Commands
{
    public class RemoveFollowCommand : DelegateCommandBase
    {
        protected override bool CanExecute(object parameter)
        {
            return parameter is Interfaces.IFollowable;
        }

        protected override async void Execute(object parameter)
        {
            if (parameter is Interfaces.IFollowable)
            {

                // TODO: MessageDialogによるフォロー解除の確認

                var followManager = HohoemaCommnadHelper.GetHohoemaApp().FollowManager;
                var followType = GetFollowItemType(parameter as Interfaces.IFollowable);
                string id;
                if (parameter is Interfaces.INiconicoContent)
                {
                    id = (parameter as Interfaces.INiconicoContent).Id;
                }
                else if (parameter is Interfaces.ISearchWithtag)
                {
                    id = (parameter as Interfaces.ISearchWithtag).Tag;
                }
                else { throw new NotSupportedException(); }

                var result = await followManager.RemoveFollow(followType, id);
            }
        }

        private Models.FollowItemType GetFollowItemType(Interfaces.IFollowable item)
        {
            if (item is Interfaces.ISearchWithtag) return Models.FollowItemType.Tag;
            if (item is Interfaces.IUser) return Models.FollowItemType.User;
            if (item is Interfaces.IMylist) return Models.FollowItemType.Mylist;
            if (item is Interfaces.ICommunity) return Models.FollowItemType.Community;

            throw new NotSupportedException();
        }
    }
}
