using Hohoema.Models.Domain.Niconico.Follow;
using Hohoema.Models.Domain.Niconico.Follow.LoginUser;
using Hohoema.Models.UseCase;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Presentation.ViewModels.Niconico.Follow
{

    public sealed class FollowContext<ItemType> : BindableBase, IFollowContext where ItemType : IFollowable
    {
        public static async Task<FollowContext<ItemType>> CreateAsync(IFollowProvider<ItemType> provider, ItemType followable)
        {
            var isFollowing = await provider.IsFollowingAsync(followable);
            return new FollowContext<ItemType>(provider, followable, isFollowing);
        }

        public static FollowContext<ItemType> Default { get; } = new();

        private readonly IFollowProvider<ItemType> _provider;
        private readonly ItemType _followable;
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

        private FollowContext(IFollowProvider<ItemType> provider, ItemType followable, bool isFollowing)
        {
            _provider = provider;
            _followable = followable;
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
                var result = await _provider.AddFollowAsync(_followable);
                if (result != Mntone.Nico2.ContentManageResult.Failed)
                {
                    IsFollowing = true;
                }
            }
            catch (Exception e)
            {
                ErrorTrackingManager.TrackError(e);
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
                var result = await _provider.RemoveFollowAsync(_followable);
                if (result == Mntone.Nico2.ContentManageResult.Success)
                {
                    IsFollowing = false;
                }
            }
            catch (Exception e)
            {
                ErrorTrackingManager.TrackError(e);
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
