using System;
using System.Collections.Generic;
using System.Text;

namespace SafeCollections_UT
{
    /// <summary>
    ///     UT object.
    /// </summary>
    internal sealed class TestObject
    {
        public int Id { get; }

        internal TestObject(int id)
        {
            Id = id;
        }
    }
}
