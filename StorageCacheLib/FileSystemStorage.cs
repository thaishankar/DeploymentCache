using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Net.Http;
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
        private static string _cacheDir;
        private uint _maxFileSizeToCacheMB;
        private long _maxCapacityBytes;
        private bool _stopRequested;
        private bool _shouldMemoryMapFiles;

        private const string CACHED_FILE_LIST_NAME = "CachedFileList.json";

        private AutoResetEvent _initiateCacheCleanup = new AutoResetEvent(false);

        // Dictionary of { siteNAme : [Zip1Metadata, Text1MetaData.. ],   ..}
        private Dictionary<string, CacheStateForSite> _cachedSites;

        // Linked list to maintain a LRU state
        private LinkedList<string> _recentlyAccessedSites;

        public static string CacheDirectory { get => _cacheDir; }

        public uint MaxFileSizeToCacheMB { get => _maxFileSizeToCacheMB; }

        public uint MaxFileSizeToCacheBytes { get => _maxFileSizeToCacheMB * 1024 * 1024; }

        public long OccupancyInBytes { get => _occupancyBytes; }

        // When we initate cache cleanup, we clean up the cache till it reaches this limit
        private long ExpectedFreeBytesAfterCleanup { get => (long)(_maxCapacityBytes * 0.8); }

        public long OccupancyInMB { get => (_occupancyBytes) / 1024 / 1024; }

        public long MaxCapacityBytes { get => _maxCapacityBytes; }

        public FileSystemStorage(string cacheDir, uint maxFileSizeToCacheMB, long maxCapacityMB, bool shouldMemoryMapFiles = true)
        {
            // Core settings/limits
            _cacheDir = cacheDir;
            _maxFileSizeToCacheMB = maxFileSizeToCacheMB;
            _maxCapacityBytes = maxCapacityMB * 1024 * 1024;
            _stopRequested = false;
            _shouldMemoryMapFiles = shouldMemoryMapFiles;

            // State initialization
            _cachedSites = new Dictionary<string, CacheStateForSite>();
            _recentlyAccessedSites = new LinkedList<string>();

            Thread cacheCleanupThread = new Thread(CacheCleanupOperation);
            cacheCleanupThread.Start();


            if (Directory.Exists(cacheDir))
            {
                HydrateStateAfterRestart();

                string json = JsonConvert.SerializeObject(_cachedSites, Formatting.Indented);
                Console.WriteLine("Cache state after rehydration : OccupancySize = {0}", _occupancyBytes);
                Console.WriteLine(json);
            }

            // Make sure cache directory exists (Will 'succeed' even if directory already exists)
            Directory.CreateDirectory(cacheDir);
        }

        private void HydrateStateAfterRestart()
        {
            // Directory.GetDirectories returns the absoulute paths
            string[] siteDirectoriesInCache = Directory.GetDirectories(CacheDirectory);

            foreach (string siteDirectory in siteDirectoriesInCache)
            {
                CacheStateForSite cacheStateForSite;
                string cachedFileListPath = Path.Combine(siteDirectory, CACHED_FILE_LIST_NAME);

                Console.WriteLine("Reading Cached Filelist for site : {0} from path: {1}", siteDirectoriesInCache, cachedFileListPath);

                if (File.Exists(cachedFileListPath))
                {
                    string cacheStateJson = File.ReadAllText(cachedFileListPath);
                    cacheStateForSite = JsonConvert.DeserializeObject<CacheStateForSite>(cacheStateJson);

                    string siteName = Path.GetFileName(siteDirectory);

                    // This should be the correct way of getting the directory name.
                    //string siteName = new DirectoryInfo(siteDirectory).Name;

                    _cachedSites.Add(siteName, cacheStateForSite);
                    Interlocked.Add(ref _occupancyBytes, cacheStateForSite.CachedFilesSize());
                }
                else
                {
                    // If we can't hydrate the state for the directory, we remove the directory from the cache
                    Directory.Delete(siteDirectory);
                }
            }
        }

        private void CacheCleanupOperation()
        {
            Console.WriteLine("Cache Cleanup Thread Started...");

            while (!_stopRequested)
            {
                _initiateCacheCleanup.WaitOne();

                while (Interlocked.Read(ref _occupancyBytes) >= ExpectedFreeBytesAfterCleanup)
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
            // Need to fetch the content from the origin
            string remoteFilePath;
            string remoteFileName;
            byte[] fileContents = null;
            CacheStateForSite cacheStateForSite = null;
            // Lock the current occupancies for the site
            lock (_cachedSites)
            {
                // If the site is currently Cached, add the file to list of files in cached for the site
                if (!_cachedSites.ContainsKey(siteName))
                {
                    _cachedSites.Add(siteName, new CacheStateForSite(siteName));
                }

                cacheStateForSite = _cachedSites[siteName];
            }

            lock (cacheStateForSite)
            {
                // This is done under lock to prevent concurrent access to site/data/SitePackages/siteversion.txt 
                if (contentType == CachedContentType.Zip)
                {
                    ContentFileHelper.GetRemoteZipFilepath(remoteContentPath, out remoteFilePath, out remoteFileName);
                }
                else
                {
                    remoteFilePath = remoteContentPath;
                    remoteFileName = Path.GetFileName(remoteFilePath);
                }

                string localDirectoryForContent = cacheStateForSite.GetPathForContentType(contentType);

                // Copy the remote file to local cache directory with the same name
                string localFilePath = Path.Combine(localDirectoryForContent, remoteFileName);

                if (!String.IsNullOrEmpty(remoteFilePath) && !String.IsNullOrEmpty(remoteFileName))
                {

                    // Only creates if Directory does not already exist. Otherwise its a no-op
                    Directory.CreateDirectory(localDirectoryForContent);

                    // Copy file from storage Volume to local Cache directory
                    // TODO: Should exception be handled here if the remote file doesn't exist
                    // TODO: Should we do more checks here to avoid overwriting if the file is unchanged?

                    FileInfo remoteFileInfo = new FileInfo(remoteFilePath);

                    if (remoteFileInfo.Length > MaxFileSizeToCacheBytes)
                    {
                        throw new Exception(string.Format("Remote File size {0} is bigger than the configured caching limit of {1}", remoteFileInfo.Length, MaxFileSizeToCacheBytes));
                    }

                    File.Copy(remoteFilePath, localFilePath, true);
                    fileContents = File.ReadAllBytes(localFilePath);

                    Interlocked.Add(ref _occupancyBytes, remoteFileInfo.Length);

                    // Not caching 2 files with the same name
                    foreach (var fileMetadata in cacheStateForSite.CachedFileList.Where(fileMetadata => fileMetadata.FileName == remoteFileName).ToList())
                    {
                        Interlocked.Add(ref _occupancyBytes, -fileMetadata.FileSize);
                    }

                    cacheStateForSite.CachedFileList.RemoveAll(fileMetadata => fileMetadata.FileName == remoteFileName);

                    FileMetadata copiedFileMetada = new FileMetadata(remoteFileName, remoteFileInfo.Length, DateTime.Now, contentType);

                    // If it is Zip, replace the current Zip file in use
                    if (contentType == CachedContentType.Zip)
                    {
                        long cacheSizeChange = cacheStateForSite.ReplaceCurrentZipFileInUse(copiedFileMetada);
                        Interlocked.Add(ref _occupancyBytes, cacheSizeChange);
                        Console.WriteLine("Cache size change after replacing zips : {0}. Sitename: {1}", cacheSizeChange, siteName);
                    }
                    else
                    {
                        cacheStateForSite.CachedFileList.Add(copiedFileMetada);
                    }

                    string cachedListForSiteJson = Path.Combine(cacheStateForSite.RootDirectoryInCache, CACHED_FILE_LIST_NAME);

                    if (File.Exists(cachedListForSiteJson))
                    {
                        File.Delete(cachedListForSiteJson);
                    }

                    string cachedFiles = JsonConvert.SerializeObject(cacheStateForSite);
                    File.WriteAllText(cachedListForSiteJson, cachedFiles);
                }

                lock (_recentlyAccessedSites)
                {
                    if (_recentlyAccessedSites.Contains(siteName))
                    {
                        _recentlyAccessedSites.Remove(siteName);
                    }

                    _recentlyAccessedSites.AddFirst(siteName);
                }
            }

            if (Interlocked.Read(ref _occupancyBytes)> MaxCapacityBytes)
            {
                _initiateCacheCleanup.Set();
            }

            Console.WriteLine("Current Occupancy after AddSite: {0}", _occupancyBytes);
            return fileContents;
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
            byte[] fileContents = null;

            if (_cachedSites.ContainsKey(siteName))
            {
                CacheStateForSite cacheStateForSite = _cachedSites[siteName];

                //try
                //{
                lock (cacheStateForSite)
                {

                    string localFilePath;

                    // For Zips we have the current fileName in cache
                    if (contentType == CachedContentType.Zip)
                    {
                        localFilePath = Path.Combine(cacheStateForSite.GetPathForContentType(contentType), cacheStateForSite.CurrentZipFile.FileName);
                    }
                    else
                    {
                        // else we get the remote fileName from the path provided
                        string remoteFileName = Path.GetFileName(remoteContentPath);
                        localFilePath = Path.Combine(cacheStateForSite.GetPathForContentType(contentType), remoteFileName);

                        if (!cacheStateForSite.IsFileCached(remoteFileName))
                        {
                            return null;
                        }
                    }

                    // If in case they site or file have been deleted before we read the contents, we catch the exception and do AddSite instead
                    fileContents = File.ReadAllBytes(localFilePath);
                }

                lock (_recentlyAccessedSites)
                {
                    if (_recentlyAccessedSites.Contains(siteName))
                    {
                        _recentlyAccessedSites.Remove(siteName);
                    }

                    _recentlyAccessedSites.AddFirst(siteName);
                }
                //}
                //catch(Exception ex)
                //{
                //    Console.WriteLine("Caught an exception getting sitecontents: {0}. Doing AddSite instead. Ex: {1}", siteName, ex.ToString());
                //    return AddSite(siteName, remoteContentPath, contentType);
                //}
            }
            else
            {
                // Requesting content for site which is not currently cached. Do Add 
                return AddSite(siteName, remoteContentPath, contentType);
            }

            return fileContents;
        }

        public void DeleteSite(string siteName)
        {
            //Console.WriteLine("Deleting Site contents for : {0} CurrentOccupancy Before Deletion: {1}", siteName, _occupancyBytes);

            CacheStateForSite cacheStateForSite = null;

            // If Site is in cache 
            lock (_cachedSites)
            {
                if (_cachedSites.ContainsKey(siteName))
                {
                    cacheStateForSite = _cachedSites[siteName];
                    _cachedSites.Remove(siteName);
                }
            }

            if (cacheStateForSite != null)
            {
                lock (cacheStateForSite)
                {
                    string siteRootPathInCache = cacheStateForSite.RootDirectoryInCache;

                    try
                    {
                        // Dispose the memory map references
                        //cacheStateForSite.DisposeMemoryMaps();

                        if (Directory.Exists(siteRootPathInCache))
                        {
                            ContentFileHelper.DeleteDirectoryHelper(cacheStateForSite.RootDirectoryInCache);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Failed to delete Directory for site : {0}.. Will retry it later. Ex: {1}", siteName, ex.ToString());
                    }

                    Interlocked.Add(ref _occupancyBytes, -cacheStateForSite.CachedFilesSize());

                    _cachedSites.Remove(siteName);

                    // In case if we fail to delete the directory, we will not remove it from the LRU list.
                    // Remove the site from LRU
                    lock (_recentlyAccessedSites)
                    {
                        if (_recentlyAccessedSites.Contains(siteName))
                        {
                            _recentlyAccessedSites.Remove(siteName);
                        }
                    }
                }
            }
        }

        public byte[] AddSiteWithUrl(string siteName, string urlToDownloadFrom, CachedContentType contentType = CachedContentType.Zip)
        {
            byte[] fileContents = null;

            if (String.IsNullOrEmpty(urlToDownloadFrom))
            {
                throw new ArgumentException("No Url Provided to download the file");
            }

            CacheStateForSite cacheStateForSite = null;

            // Lock the current occupancies for the site
            lock (_cachedSites)
            {
                // If the site is currently Cached, add the file to list of files in cached for the site
                if (!_cachedSites.ContainsKey(siteName))
                {
                    _cachedSites.Add(siteName, new CacheStateForSite(siteName, urlToDownloadFrom));
                }

                cacheStateForSite = _cachedSites[siteName];
            }

            lock (cacheStateForSite)
            {
                string localDirectoryForContent = cacheStateForSite.GetPathForContentType(contentType);

                Directory.CreateDirectory(localDirectoryForContent);

                //MemoryMappedFile memoryMapReference = null;

                string downloadNameForFile = ContentFileHelper.GetDownloadFileNameForContent(contentType);
                string outFilePath = Path.Combine(localDirectoryForContent, downloadNameForFile);

                try
                {
                    ContentFileHelper.DownloadFile(urlToDownloadFrom, outFilePath, MaxFileSizeToCacheBytes);

                    //if (_shouldMemoryMapFiles)
                    //{
                    //    memoryMapReference = MemoryMappedFile.CreateFromFile(outFilePath, FileMode.Open);
                    //}

                    fileContents = File.ReadAllBytes(outFilePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to download zip from Url for site: {0}. Ex: {1}", siteName, ex.ToString());
                }

                FileInfo localCopyInfo = new FileInfo(outFilePath);
                Interlocked.Add(ref _occupancyBytes, localCopyInfo.Length);

                FileMetadata downloadedFileMetada = new FileMetadata(downloadNameForFile, localCopyInfo.Length, DateTime.Now, contentType);

                //if (memoryMapReference != null && _shouldMemoryMapFiles)
                //{
                //    downloadedFileMetada.AddMemoryMapReference(memoryMapReference);
                //}

                // If it is Zip, replace the current Zip file in use
                if (contentType == CachedContentType.Zip)
                {
                    long cacheSizeChange = cacheStateForSite.ReplaceCurrentZipFileInUse(downloadedFileMetada);
                    Interlocked.Add(ref _occupancyBytes, cacheSizeChange);
                    Console.WriteLine("Cache size change after replacing zips with Download : {0}", cacheSizeChange);
                }
                else
                {
                    cacheStateForSite.CachedFileList.Add(downloadedFileMetada);
                }

                string cachedListForSiteJson = Path.Combine(cacheStateForSite.RootDirectoryInCache, CACHED_FILE_LIST_NAME);

                if (File.Exists(cachedListForSiteJson))
                {
                    File.Delete(cachedListForSiteJson);
                }

                string cachedFiles = JsonConvert.SerializeObject(cacheStateForSite);
                File.WriteAllText(cachedListForSiteJson, cachedFiles);
            }

            lock (_recentlyAccessedSites)
            {
                if (_recentlyAccessedSites.Contains(siteName))
                {
                    _recentlyAccessedSites.Remove(siteName);
                }

                _recentlyAccessedSites.AddFirst(siteName);
            }

            if (Interlocked.Read(ref _occupancyBytes) > MaxCapacityBytes)
            {
                _initiateCacheCleanup.Set();
            }

            Console.WriteLine("Current Occupancy: {0}", Interlocked.Read(ref _occupancyBytes));
            return fileContents;
        }

        public void ClearCache()
        {
            lock (_cachedSites)
            {
                if (Directory.Exists(_cacheDir))
                {
                    try
                    {
                        ContentFileHelper.DeleteDirectoryHelper(_cacheDir);
                    }
                    catch (IOException)
                    {
                        // Not doing anything if there is a exception deleting directory. 
                        // If for some reason delete fails, subsequent CreateDirectory will retain the directory.
                    }
                }
            }

            Directory.CreateDirectory(_cacheDir);

            _occupancyBytes = 0;
            _cachedSites.Clear();
            _recentlyAccessedSites.Clear();
        }
    }
}
