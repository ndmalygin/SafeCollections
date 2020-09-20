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
        public event EventHandler<CollectionChangedArgs<T>> CollectionChanged;

        /// <summary>
        ///     Add item to data set.
        /// </summary>
        /// <param name="item">Item.</param>
        public bool AddItem(T item)
        {
            var before = GetAll();
            try
            {
                _lock.EnterWriteLock();
                var added = _list.Add(item);

                CollectionChanged?.Invoke(this,
                    new CollectionChangedArgs<T>(new[] { item }, before, added ? CollectionChangedTypeEnum.Added :
                        CollectionChangedTypeEnum.ItemIsAlreadyExisted));

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
            var before = GetAll();
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

                CollectionChanged?.Invoke(this,
                    new CollectionChangedArgs<T>(added.ToArray(), before, CollectionChangedTypeEnum.Added));
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
            var before = GetAll();
            try
            {
                _lock.EnterWriteLock();
                var removed = _list.Remove(item);

                CollectionChanged?.Invoke(this,
                    new CollectionChangedArgs<T>(new[] { item }, before, removed ? CollectionChangedTypeEnum.Removed :
                        CollectionChangedTypeEnum.ItemNotFound));

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
            var before = GetAll();
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

                CollectionChanged?.Invoke(this,
                    new CollectionChangedArgs<T>(removed.ToArray(), before, CollectionChangedTypeEnum.Removed));
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
            var before = GetAll();
            try
            {
                _lock.EnterWriteLock();
                _list.Clear();

                CollectionChanged?.Invoke(this,
                    new CollectionChangedArgs<T>(null, before, CollectionChangedTypeEnum.Cleared));
            }
            finally
            {
                _lock.ExitWriteLock();
            }
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

        /// <summary>
        /// Free resources.
        /// </summary>
        public void Dispose()
        {
            _lock.Dispose();
        }
    }
}