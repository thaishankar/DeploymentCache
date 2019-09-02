using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using StorageCacheLib;

namespace DeploymentCacheLib
{
    // Keep one ServiceContext so as to maintain cache states.
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, 
                      ConcurrencyMode = ConcurrencyMode.Multiple,
                      IncludeExceptionDetailInFaults = true,
                      UseSynchronizationContext = true)]
    public class DeploymentCacheService : IDeploymentCacheOperations
    {
        private ContentCache contentCache = new ContentCache(@"C:\Cache", 300);

        public DeploymentCacheResponse GetZipFileForSite(DeploymentCacheRequest cacheRequest)
        {
            DeploymentCacheResponse cacheResponse = new DeploymentCacheResponse();


            cacheResponse.SiteName = cacheRequest.SiteName;

            if(contentCache.Contains(cacheRequest.RootDirectory))
            {
                cacheResponse.FileContents = contentCache.GetSiteContent(cacheRequest.RootDirectory);
            }
            else
            {
                contentCache.AddSite(cacheRequest.RootDirectory, cacheRequest.StorageVolumePath);
                cacheResponse.FileContents = contentCache.GetSiteContent(cacheRequest.RootDirectory);
            }

            cacheResponse.FileLength = cacheResponse.FileContents.Length;

            return cacheResponse;
        }

        public DeploymentCacheResponse RefreshCacheForSite(DeploymentCacheRequest cacheRequest)
        {
            DeploymentCacheResponse cacheRefreshResponse = new DeploymentCacheResponse();

            cacheRefreshResponse.SiteName = cacheRequest.SiteName;
            contentCache.UpdateSite(cacheRequest.RootDirectory, cacheRequest.StorageVolumePath);
            cacheRefreshResponse.FileContents = contentCache.GetSiteContent(cacheRequest.RootDirectory);
            cacheRefreshResponse.FileLength = cacheRefreshResponse.FileContents.Length;

            return cacheRefreshResponse;
        }

        public DeleteFromCacheResponse DeleteCacheForSite(DeploymentCacheRequest cacheRequest)
        {
            DeleteFromCacheResponse deleteFromCacheResponse = new DeleteFromCacheResponse();

            contentCache.DropSite(cacheRequest.RootDirectory);
            deleteFromCacheResponse.IsDeleteSuccessful = true;

            return deleteFromCacheResponse;
        }
    }
}
