using Hohoema.Models.Domain.Niconico.Follow;
using Hohoema.Models.Domain.Niconico.Follow.LoginUser;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Presentation.ViewModels.Niconico.Follow
{
    public sealed class FollowContext<FollowProviderType> : BindableBase
        where FollowProviderType : IFollowProvider 
    {
        public static async Task<FollowContext<FollowProviderType>> CreateAsync(FollowProviderType provider, string id)
        {
            var isFollowing = await provider.IsFollowingAsync(id);
            return new FollowContext<FollowProviderType>(provider, id, isFollowing);
        }

        public static FollowContext<FollowProviderType> Default { get; } = new();

        private readonly FollowProviderType _provider;
        private readonly string _id;
        private bool _IsFollowing;
        public bool IsFollowing
        {
            get => _IsFollowing;
            private set => SetProperty(ref _IsFollowing, value);
        }

        private bool _NowChanging;
        public bool NowChanging
        {
            get => _NowChanging;
            private set => SetProperty(ref _NowChanging, value);
        }

        private FollowContext()
        {

        }

        private FollowContext(FollowProviderType provider, string id, bool isFollowing)
        {
            _provider = provider;
            _id = id;
            IsFollowing = isFollowing;
        }


        private DelegateCommand _AddFollowCommand;
        public DelegateCommand AddFollowCommand => _AddFollowCommand
            ??= new DelegateCommand(async () => 
            {
                await AddFollowAsync();
            });

        private async Task AddFollowAsync()
        {
            NowChanging = true;
            try
            {
                var result = await _provider.AddFollowAsync(_id);
                if (result != Mntone.Nico2.ContentManageResult.Failed)
                {
                    IsFollowing = true;
                }
            }
            finally
            {
                NowChanging = false;
            }
        }

        private DelegateCommand _RemoveFollowCommand;
        public DelegateCommand RemoveFollowCommand => _RemoveFollowCommand
            ??= new DelegateCommand(async () =>
            {
                await RemoveFollowAsync();
            });

        private async Task RemoveFollowAsync()
        {
            NowChanging = true;
            try
            {
                var result = await _provider.RemoveFollowAsync(_id);
                if (result == Mntone.Nico2.ContentManageResult.Success)
                {
                    IsFollowing = false;
                }
            }
            finally
            {
                NowChanging = false;
            }
        }

        private DelegateCommand _ToggleFollowCommand;
        public DelegateCommand ToggleFollowCommand => _ToggleFollowCommand
            ??= new DelegateCommand(async () =>
            {
                if (IsFollowing)
                {
                    await RemoveFollowAsync();
                }
                else
                {
                    await AddFollowAsync();
                }
            });
    }
}
