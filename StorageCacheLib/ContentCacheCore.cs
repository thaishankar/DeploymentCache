using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace StorageCacheLib
{
    public class ContentCache : ICacheService
    {
        private FileSystemStorage _diskCacheStorage;
        private LruCache _diskCache;

        public ContentCache(string diskCacheDirectory, uint maxDiskCacheCapacityMB, uint maxFileSizeToCacheMB = 40)
        {
            // Disk cache settings/construction work
            _diskCacheStorage = new FileSystemStorage(diskCacheDirectory, maxFileSizeToCacheMB);
            _diskCache = new LruCache(_diskCacheStorage, maxDiskCacheCapacityMB);

            ClearCache();
        }

        public bool IsSiteCached(string siteName)
        {
            // TODO: Maintain state of all the type of files that we are currently caching for the site - txt, zip
            // For now, we just check if the site 
            return _diskCache.Contains(siteName);
        }

        public byte[] GetSiteContent(string siteRoot)
        {
           return _diskCache.GetSiteContent(siteRoot);
        }

        public byte[] UpdateSite(string siteRoot, string storageVolume)
        {
            return _diskCache.UpdateSite(siteRoot, storageVolume);
        }

        public byte[] AddSite(string siteRoot, string storageVolume, bool addToRamCache = false)
        {
            return _diskCache.AddSite(siteRoot, storageVolume);
        }

        public void DropSite(string siteRoot)
        {
            _diskCache.DeleteSite(siteRoot);
        }

        public void DropSites(string[] siteRootsList)
        {
            foreach (string siteRoot in siteRootsList)
            {
                DropSite(siteRoot);
            }
        }

        public void ClearCache()
        {
            _diskCache.Clear();
        }

        public CacheStats GetCacheStats()
        {
            return _diskCache.GetCacheStats();
        }
    }

    // Note: This enums are also used to create the Content Directory for the site. For zip, directory would be siteRoot/zip/
    public enum CachedContentType : uint
    {
        Zip = 0,
        Text = 1
    }

    public class CacheStats
    {
        long numberOfSitesInCache;
        long cacheCapacityBytes;
        long cacheFreeSpaceBytes;
        long cacheUsedSpaceBytes;

        public long NumberOfSitesInCache
        {
            get { return numberOfSitesInCache; }
        }
        public long CacheCapacityBytes
        {
            get { return cacheCapacityBytes; }
        }
        public long CacheFreeSpaceBytes
        {
            get { return cacheFreeSpaceBytes; }
        }
        public long CacheUsedSpaceBytes
        {
            get { return cacheUsedSpaceBytes; }
        }

        public CacheStats(long numberOfSites, long capacityBytes, long freeSpaceBytes, long usedSpaceBytes)
        {
            numberOfSitesInCache = numberOfSites;
            cacheCapacityBytes = capacityBytes;
            cacheFreeSpaceBytes = freeSpaceBytes;
            cacheUsedSpaceBytes = usedSpaceBytes;
        }
    }
}
