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
        ///     Items were removed from collection.
        /// </summary>
        Removed,

        /// <summary>
        ///     Collection was dropped.
        /// </summary>
        Cleared
    }
}