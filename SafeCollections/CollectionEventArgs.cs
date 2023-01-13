using System;

namespace SafeCollections
{
    /// <summary>
    ///     Arguments for collection events.
    /// </summary>
    /// <typeparam name="T">Generic type.</typeparam>
    public sealed class CollectionEventArgs<T> : EventArgs
    {
        /// <summary>
        ///     Instance creation constructor.
        /// </summary>
        /// <param name="items">Items.</param>
        /// <param name="collectionEventType">Event type.</param>
        public CollectionEventArgs(T[] items, CollectionEventTypeEnum collectionEventType)
        {
            Items = items;
            CollectionEventType = collectionEventType;
        }

        /// <summary>
        ///     Items.
        /// </summary>
        public T[] Items { get; }

        /// <summary>
        ///     Event type.
        /// </summary>
        public CollectionEventTypeEnum CollectionEventType { get; }
    }
}
