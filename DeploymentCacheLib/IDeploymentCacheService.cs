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
        DeploymentCacheResponse GetZipFileForSite(DeploymentCacheRequest cacheRequest);

        [OperationContract]
        CacheRefreshResponse RefreshCacheForSite(DeploymentCacheRequest cacheRequest);

        [OperationContract]
        DeleteFromCacheResponse DeleteCacheForSite(DeploymentCacheRequest cacheRequest);

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
    public class CacheRefreshResponse
    {
        string siteName;
        string fileName;
        int fileLength;
        bool isRefreshSuccessful;

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
        public bool IsRefreshSuccessful
        {
            get { return isRefreshSuccessful; }
            set { isRefreshSuccessful = value; }
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
}
