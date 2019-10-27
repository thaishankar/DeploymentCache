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
        public long FileSize;
        public string FileName;
        public DateTime VersionTimeStamp;
        public CachedContentType FileType;

        public FileMetadata(string fileName, long fileSize, DateTime versionTimeStamp, CachedContentType fileType = CachedContentType.Zip)
        {
            FileName = fileName;
            FileSize = fileSize;
            FileType = fileType;

            VersionTimeStamp = versionTimeStamp;
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
            if (ContentFileHelper.GetRemoteZipFilepath(sitePath, out absoluteFilePath, out fileName))
            {
                FileInfo siteZipInfo = new FileInfo(absoluteFilePath);
                return siteZipInfo.Length;
            }

            return -1;
        }

        public static bool GetRemoteZipFilepath(string siteRemotePath, out string absoluteRemoteFilePath, out string remoteFileName)
        {
            absoluteRemoteFilePath = "";
            remoteFileName = "";

            // Check if site Root Directory exists
            if(!Directory.Exists(siteRemotePath))
            {
                return false;
            }

            // Get the current zip filename (need to open the site's siteversion.txt or packagename.txt info files)
            string sitePackagesPath = Path.Combine(siteRemotePath, "data", "SitePackages");
            string currentZipFilePath = Path.Combine(sitePackagesPath, "siteversion.txt");

            // Read siteversion.txt
            if (!File.Exists(currentZipFilePath))
            {
                // Try the other other config file packagename.txt if siteversion.txt doesn't exist
                currentZipFilePath = Path.Combine(sitePackagesPath, "packagename.txt");
                if (!File.Exists(currentZipFilePath))
                {
                    // Neither one exists
                    return false;
                }
            }

            // Read the zip file name from siteverion.txt or packagename.txt
            using (StreamReader currentVersionConfig = new StreamReader(currentZipFilePath))
            {
                remoteFileName = currentVersionConfig.ReadLine();
                remoteFileName = remoteFileName.Trim();
            }

            absoluteRemoteFilePath = Path.Combine(sitePackagesPath, remoteFileName);

            return true;
        }
    }
}
