#nullable enable
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace System.Collections.Generic;

// A HashSet does not have a index, so all add or remove notifications will be handled with a reset notification
public sealed class ObservableHashSet<T> : HashSet<T>, INotifyCollectionChanged, INotifyPropertyChanged, ISet<T>
{
    #region Fields

    private readonly SimpleMonitor _monitor;

    #endregion

    #region Constructors

    public ObservableHashSet() : base()
    {
        _monitor = new SimpleMonitor();
    }

    public ObservableHashSet(IEnumerable<T> collection) : base(collection)
    {
        _monitor = new SimpleMonitor();
    }

    public ObservableHashSet(IEqualityComparer<T> comparer) : base(comparer)
    {
        _monitor = new SimpleMonitor();
    }

    public ObservableHashSet(IEnumerable<T> collection, IEqualityComparer<T> comparer) : base(collection, comparer)
    {
        _monitor = new SimpleMonitor();
    }

    #endregion

    #region Events

    public event NotifyCollectionChangedEventHandler? CollectionChanged;
    private event PropertyChangedEventHandler? PropertyChanged;

    event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged
    {
        add => PropertyChanged += value;
        remove => PropertyChanged -= value;
    }


    #endregion

    #region Event Methods

    private void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (CollectionChanged != null)
        {
            using (BlockReentrancy())
            {
                CollectionChanged(this, e);
            }
        }
    }

    private void OnCollectionChanged(NotifyCollectionChangedAction action, object item, int index)
    {
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, item, index));
    }

    private void OnCollectionReset()
    {
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    private void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        PropertyChanged?.Invoke(this, e);
    }

    private void OnPropertyChanged(string propertyName)
    {
        OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
    }

    #endregion

    #region Reentrancy Methods

    private IDisposable BlockReentrancy()
    {
        _monitor.Enter();
        return _monitor;
    }

    private void CheckReentrancy()
    {
        if (_monitor.Busy && (CollectionChanged != null) && (CollectionChanged.GetInvocationList().Length > 1))
        {
            throw new InvalidOperationException("There are additional attempts to change this hash set during a CollectionChanged event.");
        }
    }

    #endregion

    #region Overridden HashSet Methods

    /// <summary>
    /// Adds the specified element to a set.
    /// </summary>
    /// <param name="item">The element to add to the set.</param>
    /// <returns>true if the element is added to the <see cref="ObservableHashSet&lt;T&gt;"/> object; false if the element is already present.</returns>
    public new bool Add(T item)
    {
        CheckReentrancy();

        if (base.Add(item))
        {
            OnCollectionReset();
            OnPropertyChanged(PropertyNames.Count);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Removes all elements from a <see cref="ObservableHashSet&lt;T&gt;"/> object.
    /// </summary>
    public new void Clear()
    {
        CheckReentrancy();

        if (Count > 0)
        {
            base.Clear();
            OnCollectionReset();
            OnPropertyChanged(PropertyNames.Count);
        }
    }

    /// <summary>
    /// Removes all elements in the specified collection from the current <see cref="ObservableHashSet&lt;T&gt;"/> object.
    /// </summary>
    /// <param name="other">The collection of items to remove from the <see cref="ObservableHashSet&lt;T&gt;"/> object.</param>
    public new void ExceptWith(IEnumerable<T> other)
    {
        if (other != null)
        {
            CheckReentrancy();

            int oldCount = Count;

            base.ExceptWith(other);

            if (Count != oldCount)
            {
                OnCollectionReset();
                OnPropertyChanged(PropertyNames.Count);
            }

        }
    }

    /// <summary>
    /// Modifies the current <see cref="ObservableHashSet&lt;T&gt;"/> object to contain only elements that are present in that object and in the specified collection.
    /// </summary>
    /// <param name="other">The collection to compare to the current <see cref="ObservableHashSet&lt;T&gt;"/> object.</param>
    public new void IntersectWith(IEnumerable<T> other)
    {
        if (other != null)
        {
            CheckReentrancy();

            int oldCount = Count;

            base.IntersectWith(other);

            if (Count != oldCount)
            {
                OnCollectionReset();
                OnPropertyChanged(PropertyNames.Count);
            }

        }
    }

    /// <summary>
    /// Removes the specified element from a <see cref="ObservableHashSet&lt;T&gt;"/> object.
    /// </summary>
    /// <param name="item">The element to remove.</param>
    /// <returns>true if the element is successfully found and removed; otherwise, false. This method returns false if item is not found in the <see cref="ObservableHashSet&lt;T&gt;"/> object.</returns>
    public new bool Remove(T item)
    {
        CheckReentrancy();

        if (base.Remove(item))
        {
            OnCollectionReset();
            OnPropertyChanged(PropertyNames.Count);

            return true;
        }

        return false;
    }

    /// <summary>
    /// Modifies the current <see cref="ObservableHashSet&lt;T&gt;"/> object to contain only elements that are present either in that object or in the specified collection, but not both.
    /// </summary>
    /// <param name="other">The collection to compare to the current <see cref="ObservableHashSet&lt;T&gt;"/> object.</param>
    public new void SymmetricExceptWith(IEnumerable<T> other)
    {
        if (other != null)
        {
            CheckReentrancy();

            // A check of old count against new count is insufficient as the counts can be the same
            // Need to check whether any items are added or removed. While it would be more efficient to break
            // immediately when the criteria is satisfied, we do not know where in the changing item is.
            // A GroupBy.Contains will have to suffice for now.
            // True: Items in other collection that exist in this hashset. These are removed from the hashset.
            // False: Items in other collection that does not exist in this hashset. These are added to the hashset.
            IEnumerable<IGrouping<bool, T>> processedItems = other.GroupBy(Contains);

            base.SymmetricExceptWith(other);

            if (processedItems.Any())
            {
                OnCollectionReset();
                OnPropertyChanged(PropertyNames.Count);
            }
        }
    }

    /// <summary>
    /// Modifies the current <see cref="ObservableHashSet&lt;T&gt;"/> object to contain all elements that are present in itself, the specified collection, or both.
    /// </summary>
    /// <param name="other">The collection to compare to the current <see cref="ObservableHashSet&lt;T&gt;"/> object.</param>
    public new void UnionWith(IEnumerable<T> other)
    {
        if (other != null)
        {
            CheckReentrancy();

            int oldCount = Count;

            base.UnionWith(other);

            if (Count != oldCount)
            {
                OnCollectionReset();
                OnPropertyChanged(PropertyNames.Count);
            }
        }
    }

    public void Reset()
    {
        OnCollectionReset();
    }

    #endregion

    #region Nested Types

    private struct PropertyNames
    {
        public const string Count = "Count";
    }

    private class SimpleMonitor : IDisposable
    {
        // Fields
        private int _busyCount;

        // Methods
        public void Dispose()
        {
            _busyCount--;
        }

        public void Enter()
        {
            _busyCount++;
        }

        // Properties
        public bool Busy => _busyCount > 0;
    }

    #endregion
}