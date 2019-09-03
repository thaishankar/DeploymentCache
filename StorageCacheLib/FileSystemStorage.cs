using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StorageCacheLib
{
    internal class FileSystemStorage : ISiteCacheStorage
    {
        private long _occupancyBytes;
        private string _cacheDir;

        public string CacheDir { get => _cacheDir; }
        public long OccupancyInBytes { get => _occupancyBytes; }

        private Dictionary<string, FileMetadata> _currentOccupancies;

        public FileSystemStorage(string cacheDir)
        {
            // Core settings/limits
            _cacheDir = cacheDir;

            // State initialization
            _currentOccupancies = new Dictionary<string, FileMetadata>();

            // Make sure cache directory exists (Will 'succeed' even if directory already exists)
            Directory.CreateDirectory(cacheDir);
        }

        public long GetCacheSizeForSite(string siteRoot)
        {
            if (_currentOccupancies.ContainsKey(siteRoot))
            {
                return _currentOccupancies[siteRoot].FileSize;
            }
            else
            {
                return -1;
            }
        }

        public byte[] AddSite(string siteRoot, string storageVolume)
        {
            // Create the site subfolder in the cache
            string localSiteDirectory = GetLocalDirectoryPath(siteRoot);
            Directory.CreateDirectory(localSiteDirectory);

            // Make local copy of the current zip for the site
            string localFilePath;

            // Need to fetch the content from the origin
            string absoluteZipFilePath;
            string zipFileName;
            string sitePath = Path.Combine(storageVolume, siteRoot);

            if (ContentFileHelper.GetCurrentSiteZipFilepath(sitePath, out absoluteZipFilePath, out zipFileName))
            {
                localFilePath = Path.Combine(localSiteDirectory, zipFileName);

                // Copy file from storage Volume to local Cache directory
                File.Copy(absoluteZipFilePath, localFilePath, true);

                FileInfo localCopyInfo = new FileInfo(absoluteZipFilePath);
                _occupancyBytes += localCopyInfo.Length;

                // If the site info is currently Cached, delete the record
                if (_currentOccupancies.ContainsKey(siteRoot))
                {
                    _currentOccupancies.Remove(siteRoot);

                }

                _currentOccupancies.Add(siteRoot, new FileMetadata(zipFileName, localCopyInfo.Length));

                return File.ReadAllBytes(localFilePath);
            }

            return null;
        }

        public byte[] UpdateSite(string siteRoot, string storageVolume)
        {
            // Delete the old one if present
            DeleteSite(siteRoot);

            // Get a local copy of the newer file
            return AddSite(siteRoot, storageVolume);
        }

        public byte[] GetSiteContent(string siteRoot, int startingOffset = 0, int lengthBytes = -1)
        {
            // Get the filepath for the site's zip
            string localSiteDirectory = GetLocalDirectoryPath(siteRoot);

            if(_currentOccupancies.ContainsKey(siteRoot))
            {
                string localZipFileName = Path.Combine(localSiteDirectory, _currentOccupancies[siteRoot].FileName);

                return File.ReadAllBytes(localZipFileName);
            }

            return null;
        }

        public long DeleteSite(string siteRoot)
        {
            long oldCopySize = 0;

            // Get the filepath for the site's zip
            string localSiteDirectory = GetLocalDirectoryPath(siteRoot);

            // Delete the old one if present
            if (Directory.Exists(localSiteDirectory))
            {
                // We have the cache record for this site
                if (_currentOccupancies.ContainsKey(siteRoot))
                {
                    oldCopySize = _currentOccupancies[siteRoot].FileSize;
                    _occupancyBytes -= oldCopySize;
                    _currentOccupancies.Remove(siteRoot);

                }

                // Recursively delete all the files in the directory
                Directory.Delete(localSiteDirectory, true);
            }

            return oldCopySize;
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
            _currentOccupancies.Clear();
        }

        private string GetLocalDirectoryPath(string siteRoot)
        {
            return Path.Combine(_cacheDir, siteRoot);
        }
    }
}
