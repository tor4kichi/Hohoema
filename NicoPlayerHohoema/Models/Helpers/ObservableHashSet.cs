using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace System.Collections.Generic
{
    // A HashSet does not have a index, so all add or remove notifications will be handled with a reset notification
    public sealed class ObservableHashSet<T> : HashSet<T>, INotifyCollectionChanged, INotifyPropertyChanged, ISet<T>
    {
        #region Fields

        private SimpleMonitor _monitor;

        #endregion

        #region Constructors

        public ObservableHashSet() : base()
        {
            this._monitor = new SimpleMonitor();
        }

        public ObservableHashSet(IEnumerable<T> collection) : base(collection)
        {
            this._monitor = new SimpleMonitor();
        }

        public ObservableHashSet(IEqualityComparer<T> comparer) : base(comparer)
        {
            this._monitor = new SimpleMonitor();
        }

        public ObservableHashSet(IEnumerable<T> collection, IEqualityComparer<T> comparer) : base(collection, comparer)
        {
            this._monitor = new SimpleMonitor();
        }

        #endregion

        #region Events

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        event PropertyChangedEventHandler PropertyChanged;

        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add
            {
                PropertyChanged += value;
            }
            remove
            {
                PropertyChanged -= value;
            }
        }


        #endregion

        #region Event Methods

        void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (this.CollectionChanged != null)
            {
                using (this.BlockReentrancy())
                {
                    this.CollectionChanged(this, e);
                }
            }
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action, object item, int index)
        {
            this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, item, index));
        }

        private void OnCollectionReset()
        {
            this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, e);
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            this.OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Reentrancy Methods

        private IDisposable BlockReentrancy()
        {
            this._monitor.Enter();
            return this._monitor;
        }

        private void CheckReentrancy()
        {
            if ((this._monitor.Busy && (this.CollectionChanged != null)) && (this.CollectionChanged.GetInvocationList().Length > 1))
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
            this.CheckReentrancy();

            if (base.Add(item))
            {
                this.OnCollectionReset();
                this.OnPropertyChanged(PropertyNames.Count);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Removes all elements from a <see cref="ObservableHashSet&lt;T&gt;"/> object.
        /// </summary>
        public new void Clear()
        {
            this.CheckReentrancy();

            if (Count > 0)
            {
                base.Clear();
                this.OnCollectionReset();
                this.OnPropertyChanged(PropertyNames.Count);
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
                this.CheckReentrancy();

                int oldCount = this.Count;

                base.ExceptWith(other);

                if (this.Count != oldCount)
                {
                    this.OnCollectionReset();
                    this.OnPropertyChanged(PropertyNames.Count);
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
                this.CheckReentrancy();

                int oldCount = this.Count;

                base.IntersectWith(other);

                if (this.Count != oldCount)
                {
                    this.OnCollectionReset();
                    this.OnPropertyChanged(PropertyNames.Count);
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
            this.CheckReentrancy();

            if (base.Remove(item))
            {
                this.OnCollectionReset();
                this.OnPropertyChanged(PropertyNames.Count);

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
                this.CheckReentrancy();

                // A check of old count against new count is insufficient as the counts can be the same
                // Need to check whether any items are added or removed. While it would be more efficient to break
                // immediately when the criteria is satisfied, we do not know where in the changing item is.
                // A GroupBy.Contains will have to suffice for now.
                // True: Items in other collection that exist in this hashset. These are removed from the hashset.
                // False: Items in other collection that does not exist in this hashset. These are added to the hashset.
                var processedItems = other.GroupBy(x => this.Contains(x));

                base.SymmetricExceptWith(other);

                if (processedItems.Any())
                {
                    this.OnCollectionReset();
                    this.OnPropertyChanged(PropertyNames.Count);
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
                this.CheckReentrancy();

                int oldCount = this.Count;

                base.UnionWith(other);

                if (this.Count != oldCount)
                {
                    this.OnCollectionReset();
                    this.OnPropertyChanged(PropertyNames.Count);
                }
            }
        }

        public void Reset()
        {
            this.OnCollectionReset();
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
                this._busyCount--;
            }

            public void Enter()
            {
                this._busyCount++;
            }

            // Properties
            public bool Busy
            {
                get
                {
                    return (this._busyCount > 0);
                }
            }
        }

        #endregion
    }
}