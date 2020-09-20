namespace SafeCollections
{
    /// <summary>
    ///     Change type.
    /// </summary>
    public enum CollectionChangedTypeEnum
    {
        /// <summary>
        ///     Items were added to collection.
        /// </summary>
        Added,

        /// <summary>
        ///     Item is already existed in collection.
        /// </summary>
        ItemIsAlreadyExisted,

        /// <summary>
        ///     Items were removed from collection.
        /// </summary>
        Removed,

        /// <summary>
        ///     Item was not found in collection.
        /// </summary>
        ItemNotFound,

        /// <summary>
        ///     Collection was dropped.
        /// </summary>
        Cleared
    }
}