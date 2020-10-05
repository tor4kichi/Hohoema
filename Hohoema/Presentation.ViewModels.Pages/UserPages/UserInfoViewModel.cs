using Hohoema.Models.Domain.Niconico;

namespace Hohoema.Presentation.ViewModels.Pages.UserPages
{
    public class UserInfoViewModel : IUser
	{
        public UserInfoViewModel(string name, string id, string iconUrl = null)
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

        string INiconicoObject.Id => Id;

        string INiconicoObject.Label => Name;
    }
}
