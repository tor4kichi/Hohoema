using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.System;

namespace Hohoema.FixPrism
{
    public class DispatchObservableCollection<T> : ObservableCollection<T>
    {
        ReaderWriterLockSlim _lockSlim = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        Dictionary<DispatcherQueue, IList<NotifyCollectionChangedEventHandler>> _handlersByThread = new ();


        #region コンストラクタ
        public DispatchObservableCollection()
        {
        }
        public DispatchObservableCollection(IEnumerable<T> collection)
            : base(collection)
        {
        }
        public DispatchObservableCollection(List<T> list)
            : base(list)
        {
        }
        #endregion

        public override event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add
            {
                _lockSlim.EnterWriteLock();
                try
                {
                    var currentContext = DispatcherQueue.GetForCurrentThread();
                    if (_handlersByThread.TryGetValue(currentContext, out var list))
                    {
                        list.Add(value);
                    }
                    else
                    {
                        _handlersByThread.Add(currentContext, new List<NotifyCollectionChangedEventHandler>() { value });
                    }
                }
                finally
                {
                    _lockSlim.ExitWriteLock();
                }
            }

            remove
            {
                _lockSlim.EnterWriteLock();
                try
                {
                    foreach (var list in _handlersByThread.Values)
                    {
                        list.Remove(value);
                    }
                }
                finally
                {
                    _lockSlim.ExitWriteLock();
                }
            }
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            _lockSlim.EnterReadLock();
            try
            {
                foreach (var pair in _handlersByThread)
                {
                    var context = pair.Key;
                    var handlers = pair.Value;
                    if (context.HasThreadAccess)
                    {
                        foreach (var eventHandler in handlers.ToArray())
                        {
                            eventHandler?.Invoke(this, e);
                        }
                    }
                    else
                    {
                        context.TryEnqueue(() =>
                        {
                            _lockSlim.EnterUpgradeableReadLock();
                            try
                            {
                                foreach (var eventHandler in handlers.ToArray())
                                {
                                    eventHandler?.Invoke(this, e);
                                }
                            }
                            finally
                            {
                                _lockSlim.ExitUpgradeableReadLock();
                            }
                        });
                    }
                }
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }
    }
}
