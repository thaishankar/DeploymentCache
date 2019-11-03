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
        //private LruCache _diskCache;

        public ContentCache(string diskCacheDirectory, uint maxCapacityMB, uint maxFileSizeToCacheMB = 40)
        {
            // Disk cache settings/construction work
            _diskCacheStorage = new FileSystemStorage(diskCacheDirectory, maxFileSizeToCacheMB, maxCapacityMB);
            //_diskCache = new LruCache(_diskCacheStorage, maxDiskCacheCapacityMB);
        }

        //public bool IsSiteCached(string siteName)
        //{
        //    // TODO: Maintain state of all the type of files that we are currently caching for the site - txt, zip
        //    // For now, we just check if the site 
        //    return _diskCache.Contains(siteName);
        //}

        public byte[] GetSiteContent(string siteName, string remoteContentPath, CachedContentType contentType = CachedContentType.Zip)
        {
            return _diskCacheStorage.GetSiteContents(siteName, remoteContentPath, contentType);
        }

        public byte[] RefreshSiteContents(string siteName, string remoteContentPath, CachedContentType contentType = CachedContentType.Zip)
        {
            return _diskCacheStorage.RefreshSiteContents(siteName, remoteContentPath, contentType); ;
        }

        public byte[] AddSiteToCache(string siteName, string remoteContentPath, CachedContentType contentType = CachedContentType.Zip)
        {
            return _diskCacheStorage.AddSite(siteName, remoteContentPath, contentType);
        }

        public byte[] AddSiteWithUrlToCache(string siteName, string urlToDownloadFrom, CachedContentType contentType = CachedContentType.Zip)
        {
            return _diskCacheStorage.AddSiteWithUrl(siteName, urlToDownloadFrom);
        }

        public void RemoveSiteFromCache(string siteName)
        {
            _diskCacheStorage.DeleteSite(siteName);
        }

        public void DropSites(string[] sitesToBeDeleted)
        {
            foreach (string siteName in sitesToBeDeleted)
            {
                _diskCacheStorage.DeleteSite(siteName);
            }
        }

        public void RemoveSitesFromCache(List<string> sites)
        {
            _diskCacheStorage.ClearCache();
        }

        public void ClearCache()
        {
            _diskCacheStorage.ClearCache();
        }

        //public CacheStats GetCacheStats()
        //{
        //    return _diskCache.GetCacheStats();
        //}
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
