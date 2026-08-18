using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SafeCollections
{
    /// <summary>
    ///     Generic thread-safe collection based on Hashset with O(1) on remove / add operations.
    /// </summary>
    /// <typeparam name="T">Generic type.</typeparam>
    public class SafeList<T> : IDisposable
    {
        /// <summary>
        ///     Send collection items, before sending changes.
        /// </summary>
        private readonly bool _sendCollectionState;

        /// <summary>
        ///     Data set.
        /// </summary>
        private readonly HashSet<T> _list = [];

        /// <summary>
        ///     Thread lock.
        /// </summary>
        private readonly ReaderWriterLockSlim _lock = new();

        /// <summary>
        ///     Thread lock for events subscription.
        /// </summary>
        private readonly object _eventLock = new();

        /// <summary>
        ///     Event message for external listeners.
        /// </summary>
        private event EventHandler<CollectionEventArgs<T>> CollectionEventHandler;

        // Items count
        public int Length {
            get
            {
                try
                {
                    _lock.EnterReadLock();
                    return _list.Count;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        // Items
        public T[] Items {
            get
            {
                try
                {
                    _lock.EnterReadLock();
                   return _list.ToArray();
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        /// <summary>
        ///     Default constructor.
        /// </summary>
        public SafeList() : this(true) { }

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="sendCollectionState">If true, send collection items before sending changes.</param>
        public SafeList(bool sendCollectionState)
        {
            _sendCollectionState = sendCollectionState;
        }

        /// <summary>
        ///     Add item to data set.
        /// </summary>
        /// <param name="item">Item.</param>
        public bool AddItem(T item)
        {
            bool added;
            CollectionEventArgs<T> eventArgs;

            try
            {
                _lock.EnterWriteLock();
                added = _list.Add(item);

                eventArgs = new CollectionEventArgs<T>(
                    [item],
                    added
                        ? CollectionEventTypeEnum.Added
                        : CollectionEventTypeEnum.ItemIsAlreadyExisted
                );
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            // Invocation moved outside of WriteLock to prevent Deadlocks
            CollectionEventHandler?.Invoke(this, eventArgs);

            return added;
        }

        /// <summary>
        ///     Add items to data set.
        /// </summary>
        /// <param name="items">Items.</param>
        public void AddItems(T[] items)
        {
            var added = new List<T>();

            try
            {
                _lock.EnterWriteLock();
                foreach (var item in items)
                {
                    if (_list.Add(item))
                    {
                        added.Add(item);
                    }
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            CollectionEventHandler?.Invoke(
                this,
                new CollectionEventArgs<T>(added.ToArray(), CollectionEventTypeEnum.Added)
            );
        }

        /// <summary>
        ///     Remove item from data set.
        /// </summary>
        /// <param name="item"></param>
        /// <returns>Item.</returns>
        public bool RemoveItem(T item)
        {
            bool removed;
            CollectionEventArgs<T> eventArgs;

            try
            {
                _lock.EnterWriteLock();
                removed = _list.Remove(item);

                eventArgs = new CollectionEventArgs<T>(
                    [item],
                    removed
                        ? CollectionEventTypeEnum.Removed
                        : CollectionEventTypeEnum.ItemNotFound
                );
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            CollectionEventHandler?.Invoke(this, eventArgs);

            return removed;
        }

        /// <summary>
        ///     Remove items from data set.
        /// </summary>
        /// <param name="items">Items.</param>
        public void RemoveItems(T[] items)
        {
            var removed = new List<T>();

            try
            {
                _lock.EnterWriteLock();
                foreach (var item in items)
                {
                    if (_list.Remove(item))
                    {
                        removed.Add(item);
                    }
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            CollectionEventHandler?.Invoke(
                this,
                new CollectionEventArgs<T>(removed.ToArray(), CollectionEventTypeEnum.Removed)
            );
        }

        /// <summary>
        ///     Clear all items from data set.
        /// </summary>
        public void ClearAll()
        {
            try
            {
                _lock.EnterWriteLock();
                _list.Clear();
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            CollectionEventHandler?.Invoke(
                this,
                new CollectionEventArgs<T>(null, CollectionEventTypeEnum.Cleared)
            );
        }

        /// <summary>
        ///     Free resources.
        /// </summary>
        public void Dispose()
        {
            _lock.Dispose();
        }

        /// <summary>
        ///     Sign on events.
        /// </summary>
        /// <param name="handler">The handler.</param>
        public void SignOnEvents(EventHandler<CollectionEventArgs<T>> handler)
        {
            if (handler == null) return;
            T[] snapshot = null;

            // Lock to prevent race conditions during concurrent subscriptions
            lock (_eventLock)
            {
                // Send collection state (all items) before sending changes.
                if (_sendCollectionState)
                {
                    try
                    {
                        _lock.EnterReadLock();
                        // Take snapshot and immediately subscribe the handler inside the lock,
                        // ensuring no concurrent modifications from other threads are missed.
                        snapshot = _list.ToArray();
                        CollectionEventHandler += handler;
                    }
                    finally
                    {
                        _lock.ExitReadLock();
                    }
                }
                else
                {
                    CollectionEventHandler += handler;
                }
            }

            // Invoke the handler to deliver the initial snapshot strictly outside of any locks
            if (snapshot != null)
            {
                handler.Invoke(
                    this,
                    new CollectionEventArgs<T>(snapshot, CollectionEventTypeEnum.None)
                );
            }
        }

        /// <summary>
        ///     Unsign from events.
        /// </summary>
        /// <param name="handler">The handler.</param>
        public void UnSignFromEvents(EventHandler<CollectionEventArgs<T>> handler)
        {
            lock (_eventLock)
            {
                CollectionEventHandler -= handler;
            }
        }
    }
}
