// SPDX-License-Identifier: CC0-1.0

using System;

namespace AssetCatalog
{
    [Serializable]
    public class EntryGroup
    {
        public string groupName;
        public CatalogEntry[] entries;
    }
}
