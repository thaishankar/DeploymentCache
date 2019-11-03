using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace StorageCacheLib
{
    public static class Constants 
    {
        public const int MAX_BUFF_SIZE = 5 * 1024 * 1024;
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

        public static void DeleteDirectoryHelper(string target_dir)
        {
            string[] files = Directory.GetFiles(target_dir);
            string[] dirs = Directory.GetDirectories(target_dir);

            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (string dir in dirs)
            {
                DeleteDirectoryHelper(dir);
            }

            Directory.Delete(target_dir, false);
        }

        public static void DownloadFile(string url, string outPath, uint maxSizeToDownloadBytes)
        {
            HttpWebRequest request;
            Stream responseStream;
            byte[] data = new byte[Constants.MAX_BUFF_SIZE];
            int bytesRead = 0;
            bool fileSizeLimitExceeded = false;

            // Create HTTP web request
            if (String.IsNullOrEmpty(outPath))
            {
                throw new ArgumentException("Out File Path is Not Provided");
            }

            if(String.IsNullOrEmpty(url))
            {
                throw new ArgumentException("Url not provided to download the file");
            }

            request = WebRequest.CreateHttp(url);

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                responseStream = response.GetResponseStream();
                using (FileStream writeStream = new FileStream(outPath, FileMode.Create, FileAccess.ReadWrite))
                {
                    //Read bytes from server into 1MB buffer and write to file
                    int count = 0;
                    do
                    {
                        count = responseStream.Read(data, 0, data.Length);
                        writeStream.Write(data, 0, count);

                        bytesRead += count;

                        if(bytesRead > maxSizeToDownloadBytes)
                        {
                            fileSizeLimitExceeded = true;
                            break;
                        }
                    } while (count > 0);
                }
            }

            if (fileSizeLimitExceeded)
            {
                File.Delete(outPath);
                Console.WriteLine("File size is bigger than maximum configured download size in Cache: {0}", maxSizeToDownloadBytes);
            }
        }

        public static string GetDownloadFileNameForContent(CachedContentType contentType)
        {
            return GetCurrentTimestamp() + "." + contentType.ToString().ToLower();
        }

        private static string GetCurrentTimestamp()
        {
            return DateTime.Now.ToString("yyyyMMdd_HHmmss");
        }
    }
}
