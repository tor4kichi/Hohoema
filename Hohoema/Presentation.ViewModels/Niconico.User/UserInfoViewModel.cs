using Hohoema.Models.Domain.Niconico;
using NiconicoToolkit.User;

namespace Hohoema.Presentation.ViewModels.Niconico.User
{
    public class UserInfoViewModel : IUser
	{
        public UserInfoViewModel(string name, UserId id, string iconUrl = null)
        {
            Name = name;
            Id = id;
            IconUrl = iconUrl;
            HasIconUrl = IconUrl != null;
        }

        public string Name { get; private set; }
		public string Id { get; private set; }
		public string IconUrl { get; private set; }
		public bool HasIconUrl { get; private set; }

        UserId IUser.UserId => Id;

        string IUser.Nickname => Name;
    }
}
