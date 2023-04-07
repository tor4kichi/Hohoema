using Hohoema.Models.Domain.Niconico.Follow;
using Hohoema.Models.Domain.Niconico.Follow.LoginUser;
using Hohoema.Models.UseCase;
using Microsoft.Extensions.Logging;
using NiconicoToolkit.Account;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZLogger;

namespace Hohoema.ViewModels.Niconico.Follow
{

    public sealed class FollowContext<ItemType> : ObservableObject, IFollowContext where ItemType : IFollowable
    {
        public static async Task<FollowContext<ItemType>> CreateAsync(IFollowProvider<ItemType> provider, ItemType followable, bool? followed = null)
        {
            var isFollowing = followed ?? await provider.IsFollowingAsync(followable);
            return new FollowContext<ItemType>(provider, followable, isFollowing);
        }

        public static FollowContext<ItemType> Create(IFollowProvider<ItemType> provider, ItemType followable, bool followed)
        {
            var isFollowing = followed;
            return new FollowContext<ItemType>(provider, followable, isFollowing);
        }

        public static FollowContext<ItemType> Default { get; } = new();

        private readonly IFollowProvider<ItemType> _provider;
        private readonly ItemType _followable;
        private readonly ILogger<FollowContext<ItemType>> _logger;
        private bool _IsFollowing;
        public bool IsFollowing
        {
            get => _IsFollowing;
            set
            {
                if (SetProperty(ref _IsFollowing, value) && !NowChanging)
                {
                    if (!value)
                    {
                        _ = RemoveFollowAsync();
                    }
                    else
                    {
                        _ = AddFollowAsync();
                    }
                }
            }
        }

        private bool _NowChanging;
        public bool NowChanging
        {
            get => _NowChanging;
            private set => SetProperty(ref _NowChanging, value);
        }

        private FollowContext()
        {
            _logger = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<ILoggerFactory>().CreateLogger<FollowContext<ItemType>>();
        }

        private FollowContext(IFollowProvider<ItemType> provider, ItemType followable, bool isFollowing) : this()
        {
            _provider = provider;
            _followable = followable;
            _IsFollowing = isFollowing;
        }

        public Task ToggleFollowAsync()
        {
            if (_IsFollowing)
            {
                return RemoveFollowAsync();
            }
            else
            {
                return AddFollowAsync();
            }
        }

        private RelayCommand _AddFollowCommand;
        public RelayCommand AddFollowCommand => _AddFollowCommand
            ??= new RelayCommand(async () =>
            {
                await AddFollowAsync();
            });

        private async Task AddFollowAsync()
        {
            if (_followable == null) { return; }

            NowChanging = true;
            try
            {
                var result = await _provider.AddFollowAsync(_followable);
                if (result == ContentManageResult.Failed)
                {
                    IsFollowing = false;
                }
                else
                {
                    IsFollowing = true;
                }
            }
            catch (Exception e)
            {
                _logger.ZLogError(e, e.Message);
            }
            finally
            {
                NowChanging = false;
            }
        }

        private RelayCommand _RemoveFollowCommand;
        public RelayCommand RemoveFollowCommand => _RemoveFollowCommand
            ??= new RelayCommand(async () =>
            {
                await RemoveFollowAsync();
            });

        private async Task RemoveFollowAsync()
        {
            if (_followable == null) { return; }

            NowChanging = true;
            try
            {
                var result = await _provider.RemoveFollowAsync(_followable);
                if (result != ContentManageResult.Success)
                {
                    IsFollowing = true;
                }
                else
                {
                    IsFollowing = false;
                }
            }
            catch (Exception e)
            {
                _logger.ZLogError(e, e.Message);
            }
            finally
            {
                NowChanging = false;
            }
        }
    }
}
