using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StorageCacheLib
{
    internal class FileSystemStorage : ISiteCacheStorage
    {
        private long _occupancyBytes;
        private string _cacheDir;
        private uint _maxFileSizeToCacheMB;
        private long _maxCapacityBytes;
        private bool _stopRequested;

        private const string CACHED_FILE_LIST_NAME = "CachedFileList.json";

        private AutoResetEvent initiateCacheCleanup = new AutoResetEvent(false);

        // Dictionary of { siteNAme : [Zip1Metadata, Text1MetaData.. ],   ..}
        private Dictionary<string, List<FileMetadata>> _cachedSites;

        // Linked list to maintain a LRU state
        private LinkedList<string> _recentlyAccessedSites;

        public string CacheDir { get => _cacheDir; }

        public uint MaxFileSizeToCacheMB { get => _maxFileSizeToCacheMB;  }

        public long OccupancyInBytes { get => _occupancyBytes; }

        // When we initate cache cleanup, we clean up the cache till it reaches this limit
        private long ExpectedFreeBytesAfterCleanup { get => (long)(_maxCapacityBytes * 0.8); }

        public long OccupancyInMB { get => (_occupancyBytes / 1024 / 1024); }

        public long MaxCapacityBytes { get => _maxCapacityBytes; }

        public FileSystemStorage(string cacheDir, uint maxFileSizeToCacheMB, long maxCapacityMB)
        {
            // Core settings/limits
            _cacheDir = cacheDir;
            _maxFileSizeToCacheMB = maxFileSizeToCacheMB;
            _maxCapacityBytes = maxCapacityMB * 1024 * 1024;

            // State initialization
            _cachedSites = new Dictionary<string, List<FileMetadata>>();
            _recentlyAccessedSites = new LinkedList<string>();

            Thread cacheCleanupThread = new Thread(CacheCleanupOperation);
            cacheCleanupThread.Start();

            // Make sure cache directory exists (Will 'succeed' even if directory already exists)
            Directory.CreateDirectory(cacheDir);
        }

        public long GetCacheSizeForSite(string siteName)
        {
            if (_cachedSites.ContainsKey(siteName))
            {
                long cacheSizeForSite = 0;

                foreach(FileMetadata fileMetadata in _cachedSites[siteName])
                {
                    cacheSizeForSite += fileMetadata.FileSize;
                }

                return cacheSizeForSite;
            }
            else
            {
                return -1;
            }
        }

        public bool IsFileCachedForSite(string siteName, string fileName)
        {
            if (!_cachedSites.ContainsKey(siteName))
            {
                return false;
            }

            return _cachedSites[siteName].Any(fileMetadata => fileMetadata.FileName == fileName);
        }

        private void CacheCleanupOperation()
        {
            Console.WriteLine("Cache Cleanup Thread Started...");

            while(!_stopRequested)
            {
                initiateCacheCleanup.WaitOne();

                while (_occupancyBytes >= ExpectedFreeBytesAfterCleanup)
                {
                    LinkedListNode<string> evicteeNode = _recentlyAccessedSites.Last;

                    if (evicteeNode == null)
                    {
                        break;
                    }

                    string evicteeSite = evicteeNode.Value;

                    DeleteSite(evicteeSite);
                }

                Console.WriteLine("Cleanup operation completed... CurrentOccupancy: {0} ExpectedFreeBytes: {1}", _occupancyBytes, ExpectedFreeBytesAfterCleanup);
            }
        }

        // For Zip: remoteContentPath is the siteRoot Directory on storage volume. 
        //          This function internally handles reading the siteversion.txt/packagename.txt to get the current zip file
        // For Everything else: 
        //          Provide the exact file path from which the contents need to be retrieved on the storage volume  Eg: //FileServerIp/volume-1/webSpace/SiteRoot/Dir1/file1.txt
        public byte[] AddSite(string siteName, string remoteContentPath, CachedContentType contentType = CachedContentType.Zip)
        {
            // Create the site subfolder in the cache
            string localSiteDirectory = GetLocalDirectoryPath(siteName, contentType);

            // Only creates if Directory does not already exist. Otherwise its a no-op
            Directory.CreateDirectory(localSiteDirectory);

            // Need to fetch the content from the origin
            string remoteFilePath;
            string remoteFileName;

            if(contentType == CachedContentType.Zip)
            {
                ContentFileHelper.GetRemoteZipFilepath(remoteContentPath, out remoteFilePath, out remoteFileName);
            }
            else
            {
                remoteFilePath = remoteContentPath;
                remoteFileName = Path.GetFileName(remoteFilePath);
            }

            if (!String.IsNullOrEmpty(remoteFilePath) && !String.IsNullOrEmpty(remoteFileName))
            {
                // Copy the remote file to local cache directory with the same name
                string localFilePath = Path.Combine(localSiteDirectory, remoteFileName);

                // Copy file from storage Volume to local Cache directory
                // TODO: Should exception be handled here if the file doesn't exist
                // TODO: Should we do more checks here to avoid overwriting if the file is unchanged?
                File.Copy(remoteFilePath, localFilePath, true);

                FileInfo localCopyInfo = new FileInfo(remoteFilePath);
                _occupancyBytes += localCopyInfo.Length;

                // Lock the current occupancies for the site
                lock (_cachedSites)
                {
                    // If the site is currently Cached, add the file to list of files in cached for the site
                    if (!_cachedSites.ContainsKey(siteName))
                    {
                        _cachedSites.Add(siteName, new List<FileMetadata>());
                    }

                    _cachedSites[siteName].Add(new FileMetadata(remoteFileName, localCopyInfo.Length, DateTime.Now, contentType));

                    if (File.Exists(CACHED_FILE_LIST_NAME))
                    {
                        File.Delete(CACHED_FILE_LIST_NAME);
                    }

                    string cachedFilesForSiteJson = JsonConvert.SerializeObject(_cachedSites[siteName]);
                    string cachedFilesListFile = Path.Combine(GetSiteLocalDirectory(siteName), CACHED_FILE_LIST_NAME);
                    File.WriteAllText(cachedFilesListFile, cachedFilesForSiteJson);
                }

                lock(_recentlyAccessedSites)
                {
                    if(_recentlyAccessedSites.Contains(siteName))
                    {
                        _recentlyAccessedSites.Remove(siteName);
                    }

                    _recentlyAccessedSites.AddFirst(siteName);
                }

                return File.ReadAllBytes(localFilePath);
            }

            return null;
        }

        // For Zip: remoteContentPath is the siteRoot Directory on storage volume. 
        //          This function internally handles reading the siteversion.txt/packagename.txt to get the current zip file
        // For Everything else: 
        //          Provide the exact file path from which the contents need to be retrieved on the storage volume  Eg: //FileServerIp/volume-1/webSpace/SiteRoot/Dir1/file1.txt
        public byte[] RefreshSiteContents(string siteName, string remoteContentPath, CachedContentType contentType = CachedContentType.Zip)
        {
            return AddSite(siteName, remoteContentPath, contentType);
        }

        // This method is invoked for files which are already cached.
        public byte[] GetSiteContents(string siteName, string remoteContentPath, CachedContentType contentType = CachedContentType.Zip)
        {
            string remoteFileName = Path.GetFileName(remoteContentPath);

            // Requesting content for site which is not currently cached. Do Add Site First
            if (!IsFileCachedForSite(siteName, remoteFileName))
            {
                return AddSite(siteName, remoteContentPath, contentType);
            }

            // Get the filepath for the site's zip
            string localContentDirectory = GetLocalDirectoryPath(siteName, contentType);
            string localFilePath = Path.Combine(localContentDirectory, remoteFileName);

            byte[] fileContents = null;

            // We do not take any locks in this code path to have faster read times from cache.
            // If in case they site or file have been deleted before we read the contents, we catch the exception and do AddSite instead
            try
            {
                fileContents = File.ReadAllBytes(localFilePath);
            }
            catch(FileNotFoundException)
            {
                fileContents = AddSite(siteName, remoteContentPath, contentType);
            }
            catch(DirectoryNotFoundException)
            {
                fileContents = AddSite(siteName, remoteContentPath, contentType);
            }

            return fileContents;
        }

        public void DeleteSite(string siteName)
        {
            Console.WriteLine("Deleting Site contents for : {0} CurrentOccupancy Before Deletion: {1}", siteName, _occupancyBytes);

            // Remove the site from _cachedSites
            lock (_cachedSites)
            {
                // Delete all contents of the evicteeSite from Cache
                // TODO: Do we need a try catch here?
                Directory.Delete(GetSiteLocalDirectory(siteName), true);

                long cacheSizeForSite = GetCacheSizeForSite(siteName);

                // If Site is in cache 
                if (_cachedSites.ContainsKey(siteName) && cacheSizeForSite == -1)
                {
                    _occupancyBytes -= cacheSizeForSite;

                    _cachedSites.Remove(siteName);
                }
            }

            // Remove the site from LRU
            lock (_recentlyAccessedSites)
            {
                if (_recentlyAccessedSites.Contains(siteName))
                {
                    _recentlyAccessedSites.Remove(siteName);
                }
            }

            Console.WriteLine("Deleting Site contents for : {0} CurrentOccupancy After Deletion: {1}", siteName, _occupancyBytes);
        }

        public void ClearCache()
        {
            if (Directory.Exists(_cacheDir))
            {
                try
                {
                    Directory.Delete(_cacheDir, true);
                }
                catch(IOException)
                {
                    // Not doing anything if there is a exception deleting directory. 
                    // If for some reason delete fails, subsequent CreateDirectory will retain the directory.
                }
            }

            Directory.CreateDirectory(_cacheDir);

            _occupancyBytes = 0;
            _cachedSites.Clear();
            _recentlyAccessedSites.Clear();
        }

        private string GetLocalDirectoryPath(string siteName, CachedContentType contentType)
        {
            return Path.Combine(_cacheDir, siteName, contentType.ToString());
        }

        private string GetSiteLocalDirectory(string siteName)
        {
            return Path.Combine(_cacheDir, siteName);
        }
    }
}
