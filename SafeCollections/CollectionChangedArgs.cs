using System;

namespace SafeCollections
{
    /// <summary>
    ///     Event arguments for collection changed.
    /// </summary>
    /// <typeparam name="T">Generic type.</typeparam>
    public sealed class CollectionChangedArgs<T> : EventArgs
    {
        /// <summary>
        ///     Instance creation constructor.
        /// </summary>
        /// <param name="items">Changed items.</param>
        /// <param name="collection">Collection before change.</param>
        /// <param name="collectionChangedType">Change type.</param>
        public CollectionChangedArgs(T[] items, T[] collection, CollectionChangedTypeEnum collectionChangedType)
        {
            Items = items;
            Collection = collection;
            CollectionChangedType = collectionChangedType;
        }

        /// <summary>
        ///     Changed items.
        /// </summary>
        public T[] Items { get; }

        /// <summary>
        ///     Collection before change.
        /// </summary>
        public T[] Collection { get; }

        /// <summary>
        ///     Change type.
        /// </summary>
        public CollectionChangedTypeEnum CollectionChangedType { get; }
    }
}