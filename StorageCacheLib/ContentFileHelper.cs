using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StorageCacheLib
{
    internal class FileMetadata
    {
        private long _fileSize;
        private string _fileName;
        private DateTime _lastUpdated;

        public long FileSize
        {
            get { return _fileSize; }
            set { _fileSize = value; }
        }

        public string FileName
        {
            get { return _fileName; }
            set { _fileName = value; }
        }

        public DateTime LastUpdated
        {
            get { return _lastUpdated; }
            //set { _lastUpdated = value; }
        }

        public FileMetadata(string fileName, long fileSize)
        {
            FileName = fileName;
            FileSize = fileSize;
            _lastUpdated = DateTime.Now;
        }
    }

    public static class ContentFileHelper
    {
        public static void SplitSiteAndWebspaceFromRoot(string siteRootPath, out string webspaceGUID, out string siteGUID)
        {
            string[] siteRootParts = siteRootPath.Split(new[] { Path.DirectorySeparatorChar });

            if (siteRootParts.Length != 2)
            {
                throw new ArgumentException(string.Format("Invalid format for site Root Path '{0}' supplied", siteRootPath));
            }
            webspaceGUID = siteRootParts[0];
            siteGUID = siteRootParts[1];
        }


        public static long GetFileSizeOnStorageVolume (string siteRoot, string storageVolume)
        {
            string absoluteFilePath;
            string fileName;

            string sitePath = Path.Combine(storageVolume, siteRoot);

            // If Zip file is found
            if (ContentFileHelper.GetCurrentSiteZipFilepath(sitePath, out absoluteFilePath, out fileName))
            {
                FileInfo siteZipInfo = new FileInfo(absoluteFilePath);
                return siteZipInfo.Length;
            }

            return -1;
        }

        public static bool GetCurrentSiteZipFilepath(string sitePath, out string absoluteZipFilePath, out string zipFileName)
        {
            absoluteZipFilePath = "";
            zipFileName = "";

            // Check if site Root Directory exists
            if(!Directory.Exists(sitePath))
            {
                return false;
            }

            // Get the current zip filename (need to open the site's siteversion.txt or packagename.txt info files)
            string sitePackagesPath = Path.Combine(sitePath, "data", "SitePackages");
            string currentVersionConfigFilepath = Path.Combine(sitePackagesPath, "siteversion.txt");

            // Read siteversion.txt
            if (!File.Exists(currentVersionConfigFilepath))
            {
                // Try the other other config file packagename.txt if siteversion.txt doesn't exist
                currentVersionConfigFilepath = Path.Combine(sitePackagesPath, "packagename.txt");
                if (!File.Exists(currentVersionConfigFilepath))
                {
                    // Neither one exists
                    return false;
                }
            }

            // Read the zip file name from siteverion.txt or packagename.txt
            using (StreamReader currentVersionConfig = new StreamReader(currentVersionConfigFilepath))
            {
                zipFileName = currentVersionConfig.ReadLine();
                zipFileName = zipFileName.Trim();
            }

            absoluteZipFilePath = Path.Combine(sitePackagesPath, zipFileName);

            return true;
        }
    }
}
