﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Hohoema.FixPrism
{
    /// <summary>
    /// Implementation of <see cref="INotifyPropertyChanged"/> to simplify models.
    /// </summary>
    public abstract class BindableBase : INotifyPropertyChanged
    {
        protected BindableBase()
        {
        }

        ReaderWriterLockSlim lockSlim = new ReaderWriterLockSlim();
        Dictionary<SynchronizationContext, IList<PropertyChangedEventHandler>> _handlersByThread = new Dictionary<SynchronizationContext, IList<PropertyChangedEventHandler>>();

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged
        {
            add 
            {
                lockSlim.EnterWriteLock();
                try
                {
                    var currentContext = SynchronizationContext.Current;
                    if (_handlersByThread.TryGetValue(currentContext, out var list))
                    {
                        list.Add(value);
                    }
                    else
                    {
                        _handlersByThread.Add(currentContext, new List<PropertyChangedEventHandler>() { value });
                    }
                }
                finally
                {
                    lockSlim.ExitWriteLock();
                }
            }
            remove
            {
                lockSlim.EnterWriteLock();
                try
                {
                    var currentContext = SynchronizationContext.Current;
                    if (_handlersByThread.TryGetValue(currentContext, out var list))
                    {
                        list.Remove(value);
                    }
                }
                finally
                {
                    lockSlim.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// Checks if a property already matches a desired value. Sets the property and
        /// notifies listeners only when necessary.
        /// </summary>
        /// <typeparam name="T">Type of the property.</typeparam>
        /// <param name="storage">Reference to a property with both getter and setter.</param>
        /// <param name="value">Desired value for the property.</param>
        /// <param name="propertyName">Name of the property used to notify listeners. This
        /// value is optional and can be provided automatically when invoked from compilers that
        /// support CallerMemberName.</param>
        /// <returns>True if the value was changed, false if the existing value matched the
        /// desired value.</returns>
        protected virtual bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value)) return false;

            storage = value;
            RaisePropertyChanged(propertyName);

            return true;
        }

        protected virtual bool SetProperty<T>(ref T? storage, T? value, [CallerMemberName] string propertyName = null)
            where T : struct
        {
            if (EqualityComparer<T?>.Default.Equals(storage, value))
                return false;
            storage = value;
            RaisePropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Checks if a property already matches a desired value. Sets the property and
        /// notifies listeners only when necessary.
        /// </summary>
        /// <typeparam name="T">Type of the property.</typeparam>
        /// <param name="storage">Reference to a property with both getter and setter.</param>
        /// <param name="value">Desired value for the property.</param>
        /// <param name="propertyName">Name of the property used to notify listeners. This
        /// value is optional and can be provided automatically when invoked from compilers that
        /// support CallerMemberName.</param>
        /// <param name="onChanged">Action that is called after the property value has been changed.</param>
        /// <returns>True if the value was changed, false if the existing value matched the
        /// desired value.</returns>
        protected virtual bool SetProperty<T>(ref T storage, T value, Action onChanged, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value)) return false;

            storage = value;
            onChanged?.Invoke();
            RaisePropertyChanged(propertyName);

            return true;
        }

        /// <summary>
        /// Raises this object's PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">Name of the property used to notify listeners. This
        /// value is optional and can be provided automatically when invoked from compilers
        /// that support <see cref="CallerMemberNameAttribute"/>.</param>
        protected void RaisePropertyChanged([CallerMemberName]string propertyName = null)
        {
            lockSlim.EnterReadLock();
            try
            {
                foreach (var pair in _handlersByThread)
                {
                    var context = pair.Key;
                    var handlers = pair.Value;
                    if (SynchronizationContext.Current != context)
                    {
                        context.Post(_ =>
                        {
                            lockSlim.EnterUpgradeableReadLock();
                            try
                            {
                                var eventArgs = new PropertyChangedEventArgs(propertyName);
                                foreach (var eventHandler in handlers.ToArray())
                                {
                                    OnPropertyChanged(eventHandler, eventArgs);
                                }
                            }
                            finally
                            {
                                lockSlim.ExitUpgradeableReadLock();
                            }
                        },
                        null );
                    }
                    else
                    {
                        var eventArgs = new PropertyChangedEventArgs(propertyName);
                        foreach (var eventHandler in handlers)
                        {
                            OnPropertyChanged(eventHandler, eventArgs);
                        }
                    }
                }
            }
            finally
            {
                lockSlim.ExitReadLock();
            }
        }

        /// <summary>
        /// Raises this object's PropertyChanged event.
        /// </summary>
        /// <param name="args">The PropertyChangedEventArgs</param>
        protected virtual void OnPropertyChanged(PropertyChangedEventHandler handler, PropertyChangedEventArgs args)
        {
            handler?.Invoke(this, args);
        }
    }
}
