//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Linq;
//using System.IO;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;

//namespace StorageCacheLib
//{
//    internal class LruCache 
//    {
//        private ISiteCacheStorage _cacheStorage;
//        private long _maxCapacityBytes;

//        private ReaderWriterLockSlim _stateLock = new ReaderWriterLockSlim();
//        private LinkedList<string> _recencyState;
//        private Dictionary<string, LinkedListNode<string>> _cachedSitesDictionary;

//        public int MaxCapacityMB
//        {
//            get => (int)(_maxCapacityBytes / (1024L * 1024L));
//        }

//        public int OccupancyMB
//        {
//            get => (int)(_cacheStorage.OccupancyInBytes / (1024L * 1024L));
//        }

//        public long FreeSpaceInBytes
//        {
//            get => _maxCapacityBytes - _cacheStorage.OccupancyInBytes;
//        }

//        public LruCache(ISiteCacheStorage cacheStorage, uint maxCapacityMB)
//        {
//            // Core settings/limits
//            _maxCapacityBytes = (long)maxCapacityMB * (1024L * 1024L);

//            // State initialization
//            _cacheStorage = cacheStorage;

//            // TODO: This linked List should be a list of siteMetadata. So that the latest timestamp can be used to deleted stale entries from the Cache
//            _recencyState = new LinkedList<string>();

//            _cachedSitesDictionary = new Dictionary<string, LinkedListNode<string>>();
//        }

//        public bool Contains(string siteRoot)
//        {
//            // Check with the control state (quick lookup)
//            _stateLock.EnterReadLock();

//            bool present = false;

//            try
//            {
//                present = _cachedSitesDictionary.ContainsKey(siteRoot);
//            }
//            finally
//            {
//                _stateLock.ExitReadLock();
//            }

//            return present;
//        }

//        public byte[] AddSite(string siteRoot, string storageVolume)
//        {
//            // TODO: Check if this call can be avoided. Looks like an extra access to storage volume would be done here.
//            // Get the size of the new zip
//            long siteContentSize = ContentFileHelper.GetFileSizeOnStorageVolume(siteRoot, storageVolume);

//            if (siteContentSize == -1)
//            {
//                return null;
//            }

//            // Lock the cache state
//            _stateLock.EnterWriteLock();

//            try
//            {
//                if (_cachedSitesDictionary.ContainsKey(siteRoot))
//                {
//                    return _cacheStorage.GetSiteContent(siteRoot);
//                }

//                // Make space if needed
//                if (siteContentSize > FreeSpaceInBytes)
//                {
//                    // Need to make space -> Find which one(s) to evict
//                    long spaceFreed = TryToMakeSpace(siteContentSize);
//                }

//                // Add the site's info to the eviction control state
//                LinkedListNode<string> newSiteRecencyNode = new LinkedListNode<string>(siteRoot);

//                _recencyState.AddFirst(newSiteRecencyNode);
//                _cachedSitesDictionary.Add(siteRoot, newSiteRecencyNode);

//                // Have enough space currently
//                // Add the site to the storage component
//                return _cacheStorage.AddSite(siteRoot, storageVolume);
//            }
//            finally
//            {
//                // Unlock the cache state
//                _stateLock.ExitWriteLock();
//            }
//        }

//        public byte[] UpdateSite(string siteRoot, string storageVolume)
//        {
//            // TODO: Check if this call can be avoided. Looks like an extra access to storage volume would be done here.
//            // Get the size of the new zip
//            long newSiteContentSize = ContentFileHelper.GetFileSizeOnStorageVolume(siteRoot, storageVolume);

//            if (newSiteContentSize == -1)
//            {
//                return null;
//            }

//            // Lock the cache state
//            _stateLock.EnterWriteLock();

//            try
//            {
//                // Get the size of the old (current local) copy
//                long oldSiteContentSize;

//                // Have enough space currently
//                // Update the site's info in the eviction control state
//                LinkedListNode<string> siteRecencyNode;
//                if (_cachedSitesDictionary.ContainsKey(siteRoot))
//                {
//                    // Site is currently cached -> Get the size of the current copy
//                    oldSiteContentSize = _cacheStorage.GetCacheSizeForSite(siteRoot);

//                    siteRecencyNode = _cachedSitesDictionary[siteRoot];
//                    _recencyState.Remove(siteRecencyNode);
//                }
//                else
//                {
//                    // Site is not curently cached
//                    oldSiteContentSize = 0;
//                    siteRecencyNode = new LinkedListNode<string>(siteRoot);
//                    _cachedSitesDictionary.Add(siteRoot, siteRecencyNode);
//                }

//                // Make this a recently accessed site
//                _recencyState.AddFirst(siteRecencyNode);

//                // Figure out how much space (if any needs to be made)
//                long newSpaceNeeded = newSiteContentSize - oldSiteContentSize;

//                // Handle any evictions if needed
//                if (newSpaceNeeded > FreeSpaceInBytes)
//                {
//                    long spaceFreed = TryToMakeSpace(newSpaceNeeded);
//                }

//                // Update the site in the storage component (Update will also add if it doesn't exist)
//                return _cacheStorage.UpdateSite(siteRoot, storageVolume);
//            }
//            finally
//            {
//                _stateLock.ExitWriteLock();
//            }
//        }

//        public byte[] GetSiteContent(string siteRoot)
//        {
//            // Read lock the cache state
//            _stateLock.EnterReadLock();

//            try
//            {
//                if (_cachedSitesDictionary.ContainsKey(siteRoot))
//                {
//                    // Get the content data from disk storage
//                    byte[] content = _cacheStorage.GetSiteContent(siteRoot);

//                    return content;
//                }
//                else
//                {
//                    return null;
//                }
//            }
//            finally
//            {
//                _stateLock.ExitReadLock();
//            }
//        }

//        public void DeleteSite(string siteRoot)
//        {
//            // Lock the state (use read lock until we know it exists and has to be removed)
//            _stateLock.EnterUpgradeableReadLock();
//            try
//            {
//                if (_cachedSitesDictionary.ContainsKey(siteRoot))
//                {
//                    // Upgrade to write lock before making changes to cache state
//                    _stateLock.EnterWriteLock();
//                    // Remove/Update the site's info from the eviction control state
//                    LinkedListNode<string> siteRecencyNode = _cachedSitesDictionary[siteRoot];
//                    _recencyState.Remove(siteRecencyNode);
//                    _cachedSitesDictionary.Remove(siteRoot);

//                    // Delete the local copy from the disk storage
//                    _cacheStorage.DeleteSite(siteRoot);

//                    // Step down from write lock on the cache state
//                    _stateLock.ExitWriteLock();
//                }
//            }
//            finally
//            {
//                // Unlock the cache state
//                _stateLock.ExitUpgradeableReadLock();
//            }
//        }

//        public void Clear()
//        {
//            _stateLock.EnterWriteLock();
//            try
//            {
//                _cacheStorage.ClearCache();
//                _recencyState.Clear();
//                _cachedSitesDictionary.Clear();
//            }
//            finally
//            {
//                _stateLock.ExitWriteLock();
//            }
//        }

//        public void AdjustMaxCapacity(int newMaxCapacityMB)
//        {

//            // Lock the cache state
//            _stateLock.EnterWriteLock();

//            try
//            {
//                // Update the max capacity
//                _maxCapacityBytes = (long)newMaxCapacityMB * (1024L * 1024L);

//                // Check if space needs to be freed
//                long extraUsedBytes = _cacheStorage.OccupancyInBytes - _maxCapacityBytes;
//                if (extraUsedBytes > 0)
//                {
//                    // Free up space (if needed) to get under new max capacity
//                    TryToMakeSpace(extraUsedBytes);
//                }
//            }
//            finally
//            {
//                // Unlock the cache state
//                _stateLock.ExitWriteLock();
//            }
//        }

//        private long TryToMakeSpace(long freeSpaceNeeded)
//        {
//            long spaceFreed = 0;

//            if (freeSpaceNeeded > 0)
//            {
//                // Need to make space -> Find which one(s) to evict
//                do
//                {
//                    // Get the LRU site
//                    LinkedListNode<string> evicteeNode = _recencyState.Last;
//                    if (evicteeNode == null)
//                    {
//                        // Nothing in the recency list -> cache is empty -> nothing left to do
//                        break;
//                    }

//                    // Have a new evictee to process
//                    string evicteeSiteRoot = evicteeNode.Value;

//                    // Remove from the storage
//                    spaceFreed += _cacheStorage.DeleteSite(evicteeSiteRoot);

//                    // Remove from the control state
//                    _recencyState.Remove(evicteeNode);
//                    _cachedSitesDictionary.Remove(evicteeSiteRoot);

//                } while (spaceFreed < freeSpaceNeeded);
//            }

//            // Done
            
//            if(spaceFreed < freeSpaceNeeded)
//            {
//                throw new OutOfMemoryException(string.Format("Not enough space in cache to allocate the requested free space. FreeSpaceNeeded: {0}, SpaceFreed: {1}, CacheSize: {2}",
//                                                                freeSpaceNeeded, spaceFreed, _maxCapacityBytes));
//            }

//            return spaceFreed;
//        }

//        public CacheStats GetCacheStats()
//        {
//            CacheStats cacheStats = new CacheStats(_cachedSitesDictionary.Count, _maxCapacityBytes, FreeSpaceInBytes, _cacheStorage.OccupancyInBytes);

//            return cacheStats;
//        }
//    }
//}
