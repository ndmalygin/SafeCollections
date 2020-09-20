using System;
using System.Collections.Generic;
using System.Text;

namespace SafeCollections_UT
{
    /// <summary>
    /// UT object.
    /// </summary>
    internal sealed class DummyObject
    {
        public int Id { get; }

        internal DummyObject(int id)
        {
            Id = id;
        }
    }
}
