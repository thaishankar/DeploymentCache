using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace DeploymentCacheLib
{
    [ServiceContract]
    public interface IDeploymentCacheOperations
    {
        [OperationContract]
        [FaultContract(typeof(DeploymentCacheFault))]
        DeploymentCacheResponse GetZipFileForSite(DeploymentCacheRequest cacheRequest);

        [OperationContract]
        [FaultContract(typeof(DeploymentCacheFault))]
        DeploymentCacheResponse RefreshCacheForSite(DeploymentCacheRequest cacheRequest);

        [OperationContract]
        [FaultContract(typeof(DeploymentCacheFault))]
        DeleteFromCacheResponse DeleteCacheForSite(DeploymentCacheRequest cacheRequest);

        [OperationContract]
        [FaultContract(typeof(DeploymentCacheFault))]
        void ClearCache();

        [OperationContract]
        [FaultContract(typeof(DeploymentCacheFault))]
        DeploymentCacheStats GetDeploymentCacheStats();

        [OperationContract]
        [FaultContract(typeof(DeploymentCacheFault))]
        DeploymentCacheResponse TestDowloadSpeedForZip(DeploymentCacheRequest cacheRequest);

        // TODO: Add Async Operations for these
    }

    [DataContract]
    public class DeploymentCacheRequest
    {
        string siteName;
        string rootDirectory;
        string storageVolumePath;

        [DataMember]
        public string SiteName
        {
            get { return siteName; }
            set { siteName = value; }
        }

        [DataMember]
        public string RootDirectory
        {
            get { return rootDirectory; }
            set { rootDirectory = value; }
        }

        [DataMember]
        public string StorageVolumePath
        {
            get { return storageVolumePath; }
            set { storageVolumePath = value; }
        }
    }

    [DataContract]
    public class DeploymentCacheResponse
    {
        string siteName;
        string fileName;
        int fileLength;
        byte[] fileContents;

        [DataMember]
        public string SiteName
        {
            get { return siteName; }
            set { siteName = value; }
        }

        [DataMember]
        public string FileName
        {
            get { return fileName; }
            set { fileName = value; }
        }

        [DataMember]
        public int FileLength
        {
            get { return fileLength; }
            set { fileLength = value; }
        }

        [DataMember]
        public byte[] FileContents
        {
            get { return fileContents; }
            set { fileContents = value; }
        }
    }

    [DataContract]
    public class DeleteFromCacheResponse
    {
        string siteName;
        bool isDeleteSuccessful;

        [DataMember]
        public string SiteName
        {
            get { return siteName; }
            set { siteName = value; }
        }

        [DataMember]
        public bool IsDeleteSuccessful
        {
            get { return isDeleteSuccessful; }
            set { isDeleteSuccessful = value; }
        }
    }

    [DataContract]
    public class DeploymentCacheStats
    {
        long numberOfSitesInCache;
        long cacheCapacityBytes;
        long cacheFreeSpaceBytes;
        long cacheUsedSpaceBytes;

        [DataMember]
        public long NumberOfSitesInCache
        {
            get { return numberOfSitesInCache; }
            set { numberOfSitesInCache = value; }
        }

        [DataMember]
        public long CacheCapacityBytes
        {
            get { return cacheCapacityBytes; }
            set { cacheCapacityBytes = value; }
        }

        [DataMember]
        public long CacheFreeSpaceBytes
        {
            get { return cacheFreeSpaceBytes; }
            set { cacheFreeSpaceBytes = value; }
        }

        [DataMember]
        public long CacheUsedSpaceBytes
        {
            get { return cacheUsedSpaceBytes; }
            set { cacheUsedSpaceBytes = value; }
        }
    }

    [DataContract]
    public class DeploymentCacheFault
    {
        private string rootDirectory;
        private string storageVolumePath;
        private string details;
        private string stackTrace;

        [DataMember]
        public string RootDirectory
        {
            get { return rootDirectory; }
            set { rootDirectory = value; }
        }

        [DataMember]
        public string StorageVolumePath
        {
            get { return storageVolumePath; }
            set { storageVolumePath = value; }
        }

        [DataMember]
        public string Details
        {
            get { return details; }
            set { details = value; }
        }


        [DataMember]
        public string StackTrace
        {
            get { return stackTrace; }
            set { stackTrace = value; }
        }

        public DeploymentCacheFault(string details, string rootDirectory = "", string storageVolume = "", string stackTrace = "")
        {
            RootDirectory = rootDirectory;
            StorageVolumePath = storageVolume;
            Details = details;
            StackTrace = stackTrace;
        }
    }
}
