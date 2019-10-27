﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StorageCacheLib
{
    public interface ICacheService
    {
        byte[] GetSiteContent(string siteName, CachedContentType contentType = CachedContentType.Zip);

        byte[] RefreshSiteContents(string siteName, CachedContentType contentType = CachedContentType.Zip);

        byte[] AddSiteToCache(string siteName, string storageVolume, string rootDirectory, CachedContentType contentType = CachedContentType.Zip);

        void RemoveSiteFromCache(string siteName);

        void RemoveSitesFromCache(List<string> sites);

        void ClearCache();

        CacheStats GetCacheStats();
    }
}