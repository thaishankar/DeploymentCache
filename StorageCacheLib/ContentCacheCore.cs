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
        public const int FreeRamMinimumBufferMB = 300;
        private const int RamCheckIntervalMs = 20 * 1000;

        public int MaxDiskCacheCapacityMB { get => _diskCache.MaxCapacityMB; }
        public int DiskCacheOccupancyMB { get => _diskCache.OccupancyMB; }
        public string DiskCacheDirectory { get => _diskCacheStorage.CacheDir; }
        public int RamCacheMaxCapacity;

        private FileSystemStorage _diskCacheStorage;
        private LruCache _diskCache;
        //private RamStorage _ramCacheStorage;
        //private LruCache _ramCache;
        //private int _currentRamCacheMaxSizeMB;
        //private Timer _ramSizeCheckTimer;

        public ContentCache(string diskCacheDirectory, int maxDiskCacheCapacityMB)
        {
            // Disk cache settings/construction work
            _diskCacheStorage = new FileSystemStorage(diskCacheDirectory);
            _diskCache = new LruCache(_diskCacheStorage, maxDiskCacheCapacityMB);

            ////Ram Cache Settings/Work
            //_currentRamCacheMaxSizeMB = GetNewRamCacheMaxSizeMB();
            //_ramCacheStorage = new RamStorage();
            //Console.WriteLine("Creating RamCache with initial Capacity Limit to {0} MB", _currentRamCacheMaxSizeMB);
            //_ramCache = new LruCache(_ramCacheStorage, _currentRamCacheMaxSizeMB);
            //_ramSizeCheckTimer = new Timer(RamCacheSizeAdjuster, null, RamCheckIntervalMs, RamCheckIntervalMs);
        }
        
        public bool Contains(string siteRoot)
        {
            try
            {
                return _diskCache.Contains(siteRoot);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception checking if siteRoot in Cache: {0}. Ex: {1}", siteRoot, e.ToString());
            }

            return false;

            //// Check the RAM cache first since it's faster
            //if (_ramCache.Contains(siteRoot))
            //{
            //    return true;
            //}
            //else
            //{
            //    // Check the disk cache
            //    return _diskCache.Contains(siteRoot);
            //}
        }

        public byte[] GetSiteContent(string siteRoot)
        {
            try
            {
                return _diskCache.GetSiteContent(siteRoot);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception Getting content for siteRoot in Cache: {0}. Ex: {1}", siteRoot, e.ToString());
            }

            return null;

            //// Serve from RAM cache if it's present there
            //if (_ramCache.Contains(siteRoot))
            //{
            //    return _ramCache.GetSiteContent(siteRoot);
            //}
            //else
            //{
            //    return _diskCache.GetSiteContent(siteRoot);
            //}
        }

        public bool UpdateSite(string siteRoot, string storageVolume)
        {
            try
            {
                return _diskCache.UpdateSite(siteRoot, storageVolume);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception Updating siteRoot in Cache: {0}. Ex: {1}", siteRoot, e.ToString());
            }

            return false;

            //// Update the RAM Cache only if it exists in it currently (Since Updates will also allocate if not present yet)
            //if (_ramCache.Contains(siteRoot))
            //{
            //    byte[] newSiteContent = _diskCache.GetSiteContent(siteRoot);
            //    _ramCache.UpdateSite(siteRoot, newSiteContent);
            //}

        }

        public bool AddSite(string siteRoot, string storageVolume, bool addToRamCache = false)
        {
            try
            {
                return _diskCache.AddSite(siteRoot, storageVolume);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception Adding siteRoot in Cache: {0}. Ex: {1}", siteRoot, e.ToString());
            }

            return false;


            //// Only directly add to the RAM cache if requested since it's a lot smaller
            //if (addToRamCache)
            //{
            //    byte[] siteContent = _diskCache.GetSiteContentByTag(siteRoot);
            //    _ramCache.AddSite(siteRoot, siteContent);
            //}
        }

        /// <summary>
        /// Will attempt to add new sites from provided list.
        /// Sites are added in order with lower indices having priority if there is not enough capacity for the whole list
        /// </summary>
        /// <param name="siteRootsList">List needs to be in sorted order with highest priority sites first</param>
        //public int AddSites(string[] siteRootsList, string storageVolume, bool makeSpace = false)
        //{
        //long freeDiskCacheCapacity;
        //long newSiteSize;
        //List<string> addedSites = new List<string>();
        //foreach (string newSiteRoot in siteRootsList)
        //{
        //    if (!makeSpace)
        //    {
        //        freeDiskCacheCapacity = (_diskCache.MaxCapacityMB - _diskCache.OccupancyMB) * (1024 * 1024);
        //        newSiteSize = ContentFileHelper.GetCurrentSiteSize(newSiteRoot, storageVolume);

        //        if(newSiteSize == -1)
        //        {
        //            continue;
        //        }

        //        if (newSiteSize > freeDiskCacheCapacity)
        //        {
        //            // Disk Cache is full
        //            break;
        //        }
        //    }

        //    // Add the site to the disk cache
        //    _diskCache.AddSite(newSiteRoot);
        //    addedSites.Add(newSiteRoot);
        //}

        //return addedSites.Count;
        //}

        public void DropSite(string siteRoot)
        {
            //// Remove from RAM cache if it exists there
            //_ramCache.DeleteSite(siteRoot);

            // Remove from Disk cache
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
            //// Clear the RAM cache
            //_ramCache.Clear();

            // Clear the Disk cache
            _diskCache.Clear();
        }

        //private int GetNewRamCacheMaxSizeMB()
        //{
        //    PerformanceCounter availableMemoryCounter = new PerformanceCounter("Memory", "Available MBytes");
        //    int currentAvailableMemoryMB = Convert.ToInt32(availableMemoryCounter.NextValue());

        //    return currentAvailableMemoryMB - FreeRamMinimumBufferMB + _ramCache.OccupancyMB;
        //}

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="unusedState">Not used but required to be present for timer callback usage</param>
        //private void RamCacheSizeAdjuster(object unusedState)
        //{
        //    int newRamCacheMaxSizeMB = GetNewRamCacheMaxSizeMB();
        //    Console.WriteLine("Updating RamCache Capacity Limit to {0} MB", newRamCacheMaxSizeMB);

        //    _ramCache.AdjustMaxCapacity(newRamCacheMaxSizeMB);
        //    _currentRamCacheMaxSizeMB = newRamCacheMaxSizeMB;
        //}
    }
}
