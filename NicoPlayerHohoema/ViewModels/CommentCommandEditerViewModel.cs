using Mntone.Nico2.Videos.Comment;
using NicoPlayerHohoema.Models;
using Prism.Commands;
using Prism.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using NicoPlayerHohoema.Views;
using System.Reactive.Concurrency;
using System.Threading;

namespace NicoPlayerHohoema.ViewModels
{
	public class CommentCommandEditerViewModel : BindableBase, IDisposable
	{
        private SynchronizationContextScheduler _PlayerWindowUIDispatcherScheduler;
        public SynchronizationContextScheduler PlayerWindowUIDispatcherScheduler
        {
            get
            {
                return _PlayerWindowUIDispatcherScheduler
                    ?? (_PlayerWindowUIDispatcherScheduler = new SynchronizationContextScheduler(SynchronizationContext.Current));
            }
        }


        public static IReadOnlyList<CommandType?> SizeCommandItems { get; private set; }
		public static IReadOnlyList<CommandType?> AlingmentCommandItems { get; private set; }
		public static IReadOnlyList<CommandType?> ColorCommandItems { get; private set; }
		public static IReadOnlyList<CommandType?> ColorPremiumCommandItems { get; private set; }

		static CommentCommandEditerViewModel()
		{
			SizeCommandItems = new List<CommandType?>()
			{
				CommandType.big,
				CommandType.medium,
				CommandType.small
			};

			AlingmentCommandItems = new List<CommandType?>()
			{
				CommandType.ue,
				CommandType.naka,
				CommandType.shita
			};

			ColorCommandItems = new List<CommandType?>()
			{
				CommandType.white, 
				CommandType.red,
				CommandType.pink,
				CommandType.orange,
				CommandType.yellow,
				CommandType.green,
				CommandType.cyan,
				CommandType.blue,
				CommandType.purple,
				CommandType.black,
			};

			ColorPremiumCommandItems = new List<CommandType?>()
			{
				CommandType.white2,
				CommandType.red2,
				CommandType.pink2,
				CommandType.orange2,
				CommandType.yellow2,
				CommandType.green2,
				CommandType.cyan2,
				CommandType.blue2,
				CommandType.purple2,
				CommandType.black2,
			};
		}


		public event Action OnCommandChanged;



        public ReactiveProperty<bool> IsCanNot184 { get; private set; }
        public ReactiveProperty<bool> CanChangeAnonymity { get; private set; }
		public ReactiveProperty<bool> IsAnonymousComment { get; private set; }

		public ReactiveProperty<CommandType?> SizeSelectedItem { get; private set; }
		public ReactiveProperty<CommandType?> AlingmentSelectedItem { get; private set; }
		public ReactiveProperty<CommandType?> ColorSelectedItem { get; private set; }

		public ReactiveProperty<Color> FreePickedColor { get; private set; }
		public ReactiveProperty<bool> IsPickedColor { get; private set; }

		private bool _IsPremiumUser;
		public bool IsPremiumUser
		{
			get { return _IsPremiumUser; }
			set { SetProperty(ref _IsPremiumUser, value); }
		}

		private bool _NowReseting;
		public DelegateCommand ResetAllCommand { get; private set; }


		public bool IsAnonymousDefault { get; set; }


		private CompositeDisposable _CompositeDisposable;

		public CommentCommandEditerViewModel()
		{
			_CompositeDisposable = new CompositeDisposable();

            IsCanNot184 = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler);
            CanChangeAnonymity = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler, false)
				.AddTo(_CompositeDisposable);
			IsAnonymousComment = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler, IsAnonymousDefault)
				.AddTo(_CompositeDisposable);

			SizeSelectedItem = new ReactiveProperty<CommandType?>(PlayerWindowUIDispatcherScheduler)
				.AddTo(_CompositeDisposable);
			AlingmentSelectedItem = new ReactiveProperty<CommandType?>(PlayerWindowUIDispatcherScheduler)
				.AddTo(_CompositeDisposable);
			ColorSelectedItem = new ReactiveProperty<CommandType?>(PlayerWindowUIDispatcherScheduler)
				.AddTo(_CompositeDisposable);

			FreePickedColor = new ReactiveProperty<Color>(PlayerWindowUIDispatcherScheduler)
				.AddTo(_CompositeDisposable);
			IsPickedColor = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler, false)
				.AddTo(_CompositeDisposable);

			ResetAllCommand = new DelegateCommand(() => 
			{
				_NowReseting = true;

				try
				{
					IsAnonymousComment.Value = IsAnonymousDefault;
					SizeSelectedItem.Value = null;
					AlingmentSelectedItem.Value = null;
					ColorSelectedItem.Value = null;
					FreePickedColor.Value = default(Color);
					IsPickedColor.Value = false;
				}
				finally
				{
					_NowReseting = false;
				}

				OnCommandChanged?.Invoke();
			});


			Observable.Merge(
				IsAnonymousComment.ToUnit(),
				SizeSelectedItem.ToUnit(),
				AlingmentSelectedItem.ToUnit(),
				ColorSelectedItem.ToUnit(),
				FreePickedColor.ToUnit(),
				IsPickedColor.ToUnit()
				)
				.Where(x => !_NowReseting)
				.Subscribe(_ => OnCommandChanged?.Invoke())
				.AddTo(_CompositeDisposable);
		}



		public void Dispose()
		{
			_CompositeDisposable.Dispose();
		}

        



		public void ChangeEnableAnonymity(bool enableAnonymsouUser)
		{
			CanChangeAnonymity.Value = enableAnonymsouUser;

			if (!enableAnonymsouUser)
			{
                IsAnonymousDefault = false;
				IsAnonymousComment.Value = false;
			}
		}		


		public IEnumerable<CommandType> MakeCommands()
		{
			if (IsAnonymousComment.Value)
			{
				yield return CommandType._184;
			}

			if (SizeSelectedItem.Value.HasValue)
			{
				yield return SizeSelectedItem.Value.Value;
			}

			if (AlingmentSelectedItem.Value.HasValue)
			{
				yield return AlingmentSelectedItem.Value.Value;
			}

			if (IsPickedColor.Value)
			{
				
			}
			else if (ColorSelectedItem.Value.HasValue)
			{
				yield return ColorSelectedItem.Value.Value;
			}
		}

		

		public string MakeCommandsString()
		{
			List<string> commands = new List<string>();

			if (IsAnonymousComment.Value)
			{
				commands.Add("184");
			}

			if (SizeSelectedItem.Value.HasValue)
			{
				commands.Add(SizeSelectedItem.Value.Value.ToString());
			}

			if (AlingmentSelectedItem.Value.HasValue)
			{
				commands.Add(AlingmentSelectedItem.Value.Value.ToString());
			}

			if (IsPickedColor.Value)
			{
				commands.Add(FreePickedColor.Value.ToString());
			}
			else if (ColorSelectedItem.Value.HasValue)
			{
				commands.Add(ColorSelectedItem.Value.Value.ToString());
			}

			return String.Join(" ", commands);
		}

		
	}
}
