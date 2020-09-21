namespace SafeCollections
{
    /// <summary>
    ///     Event type.
    /// </summary>
    public enum CollectionEventTypeEnum
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
        Cleared,

        /// <summary>
        ///     No changes.
        /// </summary>
        None
    }
}