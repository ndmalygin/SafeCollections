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
    public class SafeList<T>
    {
        /// <summary>
        ///     Data set.
        /// </summary>
        private readonly HashSet<T> _list = new HashSet<T>();

        /// <summary>
        ///     Thread lock.
        /// </summary>
        private readonly AutoResetEvent _lock = new AutoResetEvent(true);

        /// <summary>
        ///     Event message for external listeners.
        /// </summary>
        public event EventHandler<CollectionChangedArgs<T>> CollectionChanged;

        /// <summary>
        ///     Add item to data set.
        /// </summary>
        /// <param name="item">Item.</param>
        public void AddItem(T item)
        {
            _lock.WaitOne();
            var before = _list.ToArray();
            _list.Add(item);
            CollectionChanged?.Invoke(this,
                new CollectionChangedArgs<T>(new[] {item}, before, CollectionChangedTypeEnum.Added));
            _lock.Set();
        }

        /// <summary>
        ///     Add items to data set.
        /// </summary>
        /// <param name="items">Items.</param>
        public void AddItems(T[] items)
        {
            _lock.WaitOne();
            var before = _list.ToArray();
            foreach (var item in items) _list.Add(item);
            CollectionChanged?.Invoke(this,
                new CollectionChangedArgs<T>(items, before, CollectionChangedTypeEnum.Added));
            _lock.Set();
        }

        /// <summary>
        ///     Remove item from data set.
        /// </summary>
        /// <param name="item"></param>
        /// <returns>Item.</returns>
        public bool RemoveItem(T item)
        {
            _lock.WaitOne();
            var before = _list.ToArray();
            var removed = _list.Remove(item);
            if (removed)
                CollectionChanged?.Invoke(this,
                    new CollectionChangedArgs<T>(new[] {item}, before, CollectionChangedTypeEnum.Removed));
            _lock.Set();

            return removed;
        }

        /// <summary>
        ///     Remove items from data set.
        /// </summary>
        /// <param name="items">Items.</param>
        public void RemoveItems(T[] items)
        {
            _lock.WaitOne();
            var before = _list.ToArray();
            var removed = new List<T>();
            foreach (var item in items)
                if (_list.Remove(item))
                    removed.Add(item);
            CollectionChanged?.Invoke(this,
                new CollectionChangedArgs<T>(removed.ToArray(), before, CollectionChangedTypeEnum.Removed));
            _lock.Set();
        }

        /// <summary>
        ///     Clear all items from data set.
        /// </summary>
        public void ClearAll()
        {
            _lock.WaitOne();
            var before = _list.ToArray();
            _list.Clear();
            CollectionChanged?.Invoke(this,
                new CollectionChangedArgs<T>(null, before, CollectionChangedTypeEnum.Cleared));
            _lock.Set();
        }
    }
}