#nullable enable
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Reactive.Subjects;

namespace Hohoema;

public static class MessengerObservableExtensions
{
    public static IObservable<TMessage> ObserveMessage<TMessage>(this IMessenger messenger, object recipient) where TMessage : class
    {
        return new MessageObserver<TMessage>(messenger, recipient);
    }

    public class MessageObserver<TMessage> : SubjectBase<TMessage> where TMessage : class
    {
        private readonly IMessenger _messenger;
        private readonly object _recipient;
        private readonly Subject<TMessage> _subject;

        public MessageObserver(IMessenger messenger, object recipient)
        {
            _subject = new Subject<TMessage>();
            _messenger = messenger;
            _recipient = recipient;
            _messenger.Register<TMessage>(_recipient, (m, r) =>
            {
                _subject.OnNext(r);
            });
        }

        public override bool HasObservers => _subject.HasObservers;

        public override bool IsDisposed => _subject.IsDisposed;

        public override void Dispose()
        {
            if (_messenger.IsRegistered<TMessage>(_recipient))
            {
                _messenger.Unregister<TMessage>(_recipient);
            }
            _subject.Dispose();
        }

        public override void OnCompleted()
        {
            if (_messenger.IsRegistered<TMessage>(_recipient))
            {
                _messenger.Unregister<TMessage>(_recipient);
            }
            _subject.OnCompleted();
        }

        public override void OnError(Exception error)
        {
            _subject.OnError(error);
        }

        public override void OnNext(TMessage value)
        {
            _subject.OnNext(value);
        }

        public override IDisposable Subscribe(IObserver<TMessage> observer)
        {
            return _subject.Subscribe(observer);
        }
    }
}
