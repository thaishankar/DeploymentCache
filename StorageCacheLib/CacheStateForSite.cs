using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StorageCacheLib
{
    internal class FileMetadata
    {
        public long FileSize;
        public string FileName;
        public DateTime VersionTimeStamp;
        public CachedContentType FileType;
        //public bool IsMemoryMapped;
        //public MemoryMappedFile MemoryMapReference;

        public FileMetadata(string fileName, long fileSize, DateTime versionTimeStamp, CachedContentType fileType = CachedContentType.Zip)
        {
            FileName = fileName;
            FileSize = fileSize;
            FileType = fileType;

            VersionTimeStamp = versionTimeStamp;
        }

        //public void AddMemoryMapReference(MemoryMappedFile memoryMapReference)
        //{                                                                                       
        //    if(memoryMapReference == null)
        //    {
        //        throw new ArgumentNullException("Memory Map Reference for file {0} is null", FileName);
        //    }

        //    IsMemoryMapped = true;
        //    MemoryMapReference = memoryMapReference;
        //}
    }

    internal class CacheStateForSite
    {
        private string SiteName;

        public List<FileMetadata> CachedFileList;
        public string RootDirectoryInCache;
        public bool IsSiteWithZipUrl;
        public string UrlForZip;
        public FileMetadata CurrentZipFile;

        private CacheStateForSite()
        {

        }

        public CacheStateForSite(string siteName)
        {
            SiteName = siteName;
            CachedFileList = new List<FileMetadata>();
            RootDirectoryInCache = Path.Combine(FileSystemStorage.CacheDirectory, siteName);
            CurrentZipFile = null;
        }

        public CacheStateForSite(string siteName, string urlForZip) 
            : this(siteName)
        {
            IsSiteWithZipUrl = true;
            UrlForZip = urlForZip;
        }

        public long CachedFilesSize()
        {
            long cacheSizeForSite = 0;

            foreach (FileMetadata fileMetadata in CachedFileList)
            {
                cacheSizeForSite += fileMetadata.FileSize;
            }

            return cacheSizeForSite;
        }

        public bool IsFileCached(string fileName)
        {
            foreach(FileMetadata fileMetadata in CachedFileList)
            {
                if(String.Equals(fileName, fileMetadata.FileName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        public string GetPathForContentType(CachedContentType contentType)
        {
            return Path.Combine(RootDirectoryInCache, contentType.ToString());
        }

        //public void DisposeMemoryMaps()
        //{
        //    foreach (FileMetadata fileMetadata in CachedFileList)
        //    {
        //        if(fileMetadata.IsMemoryMapped)
        //        {
        //            fileMetadata.MemoryMapReference.Dispose();
        //        }
        //    }
        //}

        // Returns the size change in bytes after replacing the zip files
        public long ReplaceCurrentZipFileInUse(FileMetadata newZipFile)
        {
            FileMetadata oldZipFile = CurrentZipFile;

            long changeInSize = 0;

            // This is the case when old zip file exists
            if (oldZipFile != null)
            {
                // Delete the old file
                string oldZipRootDirectory = GetPathForContentType(oldZipFile.FileType);
                string oldZipPath = Path.Combine(oldZipRootDirectory, oldZipFile.FileName);

                // If the names for oldZipFile and newZipFile is same, File.Copy has handled the replacing of zip files.
                if (oldZipFile.FileName != newZipFile.FileName)
                {
                    File.Delete(oldZipPath);
                }

                // Remove old file from cachedFileList
                CachedFileList.RemoveAll(fileMetadata => fileMetadata.FileName == oldZipFile.FileName);
                changeInSize = newZipFile.FileSize - oldZipFile.FileSize;
            }

            // Add the new file to cachedFileList
            CachedFileList.Add(newZipFile);

            CurrentZipFile = newZipFile;

            return changeInSize;
        }
    }
}
