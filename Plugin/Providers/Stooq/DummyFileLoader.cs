using System.Collections.Generic;
using System.Linq;

namespace AmiBroker.Plugin.Providers.Stooq
{
    class DummyFileLoader : FileLoader
    {
        internal DummyFileLoader(Configuration configuration) : base(configuration)
        {
        }

        internal override IEnumerable<string> Load()
        {
            return Enumerable.Empty<string>();
        }
    }
}