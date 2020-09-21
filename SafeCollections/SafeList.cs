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
        private readonly HashSet<T> _list = new HashSet<T>();

        /// <summary>
        ///     Thread lock.
        /// </summary>
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        /// <summary>
        ///     Event message for external listeners.
        /// </summary>
        private event EventHandler<CollectionEventArgs<T>> CollectionEventHandler;

        /// <summary>
        ///     Default constructor.
        /// </summary>
        public SafeList() : this(true)
        {
        }

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
            try
            {
                _lock.EnterWriteLock();
                var added = _list.Add(item);

                CollectionEventHandler?.Invoke(this,new CollectionEventArgs<T>(new[] { item }, added ? CollectionEventTypeEnum.Added :
                        CollectionEventTypeEnum.ItemIsAlreadyExisted));

                return added;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        ///     Add items to data set.
        /// </summary>
        /// <param name="items">Items.</param>
        public void AddItems(T[] items)
        {
            try
            {
                var added = new List<T>();
                _lock.EnterWriteLock();
                foreach (var item in items)
                {
                    if (_list.Add(item))
                    {
                        added.Add(item);
                    }
                }

                CollectionEventHandler?.Invoke(this,new CollectionEventArgs<T>(added.ToArray(), CollectionEventTypeEnum.Added));
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        ///     Remove item from data set.
        /// </summary>
        /// <param name="item"></param>
        /// <returns>Item.</returns>
        public bool RemoveItem(T item)
        {
            try
            {
                _lock.EnterWriteLock();
                var removed = _list.Remove(item);

                CollectionEventHandler?.Invoke(this,new CollectionEventArgs<T>(new[] { item }, removed ? CollectionEventTypeEnum.Removed :
                        CollectionEventTypeEnum.ItemNotFound));

                return removed;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        ///     Remove items from data set.
        /// </summary>
        /// <param name="items">Items.</param>
        public void RemoveItems(T[] items)
        {
            try
            {
                _lock.EnterWriteLock();
                var removed = new List<T>();
                foreach (var item in items)
                {
                    if (_list.Remove(item))
                    {
                        removed.Add(item);
                    }
                }

                CollectionEventHandler?.Invoke(this,new CollectionEventArgs<T>(removed.ToArray(), CollectionEventTypeEnum.Removed));
            }
            finally
            {
                _lock.ExitWriteLock();
            }
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

                CollectionEventHandler?.Invoke(this, new CollectionEventArgs<T>(null, CollectionEventTypeEnum.Cleared));
            }
            finally
            {
                _lock.ExitWriteLock();
            }
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
            // Send collection state (all items) before sending changes.
            if (_sendCollectionState)
            {
                handler.Invoke(this, new CollectionEventArgs<T>(GetAll(), CollectionEventTypeEnum.None));
            }

            CollectionEventHandler += handler;
        }

        /// <summary>
        ///     Unsign from events.
        /// </summary>
        /// <param name="handler">The handler.</param>
        public void UnSignFromEvents(EventHandler<CollectionEventArgs<T>> handler)
        {
            CollectionEventHandler -= handler;
        }

        /// <summary>
        ///     Get all data set items.
        /// </summary>
        /// <returns></returns>
        private T[] GetAll()
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
}