using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace StorageCacheLib
{
    public class ContentCache
    {
        private FileSystemStorage _diskCacheStorage;
        private LruCache _diskCache;

        public ContentCache(string diskCacheDirectory, int maxDiskCacheCapacityMB)
        {
            // Disk cache settings/construction work
            _diskCacheStorage = new FileSystemStorage(diskCacheDirectory);
            _diskCache = new LruCache(_diskCacheStorage, maxDiskCacheCapacityMB);

            ClearCache();
        }
        
        public bool Contains(string siteRoot)
        {
            return _diskCache.Contains(siteRoot);
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
