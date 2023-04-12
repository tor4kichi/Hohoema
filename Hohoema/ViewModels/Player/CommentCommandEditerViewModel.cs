#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using Hohoema.Models.Niconico;
using Hohoema.Models.Player;
using NiconicoToolkit.Comment;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Hohoema.ViewModels;

public class CommentCommandEditerViewModel : ObservableObject, IDisposable
	{
		public PlayerSettings PlayerSettings { get; }
		public NiconicoSession NiconicoSession { get; }

		public ReactiveProperty<bool> IsAnonymousCommenting { get; }

		public ReactiveProperty<CommandType?> SelectedCommentSize { get; }
		public ReactiveProperty<CommandType?> SelectedAlingment { get; }
		public ReactiveProperty<CommandType?> SelectedColor { get; }
		public ReactiveProperty<bool> IsUseCustomCommandText { get; }
		public ReactiveProperty<string> CustomCommandText { get; }


		CompositeDisposable _disposables = new CompositeDisposable();

		public CommentCommandEditerViewModel(
			PlayerSettings playerSettings,
			NiconicoSession niconicoSession,
			IScheduler scheduler
			)
		{
			_disposables = new CompositeDisposable();

			PlayerSettings = playerSettings;
			NiconicoSession = niconicoSession;

			IsAnonymousCommenting = new ReactiveProperty<bool>(scheduler, PlayerSettings.IsDefaultCommentWithAnonymous)
				.AddTo(_disposables);
			SelectedCommentSize = new ReactiveProperty<CommandType?>(scheduler)
				.AddTo(_disposables);
			SelectedAlingment = new ReactiveProperty<CommandType?>(scheduler)
				.AddTo(_disposables);
			SelectedColor = new ReactiveProperty<CommandType?>(scheduler)
				.AddTo(_disposables);
			IsUseCustomCommandText = new ReactiveProperty<bool>(scheduler)
				.AddTo(_disposables);

			CustomCommandText = new ReactiveProperty<string>(scheduler)
				.AddTo(_disposables);

			CommandsText = new[]
			{
				IsAnonymousCommenting.ToUnit(),
				SelectedCommentSize.ToUnit(),
				SelectedAlingment.ToUnit(),
				SelectedColor.ToUnit(),
				IsUseCustomCommandText.ToUnit(),
				CustomCommandText.ToUnit()
			}
			.Merge()
			.Throttle(TimeSpan.FromSeconds(0.1))
			.Select(_ => MakeCommandsString())
			.ToReadOnlyReactiveProperty(eventScheduler: scheduler)
			.AddTo(_disposables);
		}

		public IReadOnlyReactiveProperty<string> CommandsText { get; }


		string MakeCommandsString()
		{
			List<string> commands = new List<string>();

			if (IsAnonymousCommenting.Value)
			{
				commands.Add("184");
			}

			if (SelectedCommentSize.Value is CommandType sizeCommand)
			{
				commands.Add(sizeCommand.ToString());
			}

			if (SelectedAlingment.Value is CommandType alignmentCommand)
			{
				commands.Add(alignmentCommand.ToString());
			}

			if (SelectedColor.Value is CommandType colorType)
			{
				commands.Add(colorType.ToString());
			}

			if (IsUseCustomCommandText.Value
				&& !string.IsNullOrWhiteSpace(CustomCommandText.Value)
				)
			{
				commands.Add(CustomCommandText.Value);
			}

			return String.Join(' ', commands.Distinct());
		}

    public void Dispose()
    {
        ((IDisposable)_disposables).Dispose();
    }
}
