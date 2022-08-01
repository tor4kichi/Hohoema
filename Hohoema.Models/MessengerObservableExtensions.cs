using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema
{
    public static class MessengerObservableExtensions
    {
        public static MessageObserver<TMessage> ObserveMessage<TMessage>(this IMessenger messenger) where TMessage : class
        {
            return new MessageObserver<TMessage>(messenger);
        }

        public class MessageObserver<TMessage> : SubjectBase<TMessage>, IRecipient<TMessage> where TMessage : class
        {
            private readonly IMessenger _messenger;
            private readonly Subject<TMessage> _subject;

            public MessageObserver(IMessenger messenger)
            {
                _subject = new Subject<TMessage>();
                _messenger = messenger;
            }

            void IRecipient<TMessage>.Receive(TMessage message)
            {
                OnNext(message);
            }

            public override bool HasObservers => _subject.HasObservers;

            public override bool IsDisposed => _subject.IsDisposed;

            public override void Dispose()
            {
                if (_messenger.IsRegistered<TMessage>(this))
                {
                    _messenger.Unregister<TMessage>(this);
                }
                _subject.Dispose();
            }

            public override void OnCompleted()
            {
                if (_messenger.IsRegistered<TMessage>(this))
                {
                    _messenger.Unregister<TMessage>(this);
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
                if (_messenger.IsRegistered<TMessage>(this) is false)
                {
                    _messenger.Register<TMessage>(this);
                }

                return _subject.Subscribe(observer);
            }
        }
    }
}
