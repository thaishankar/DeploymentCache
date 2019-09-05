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
                      IncludeExceptionDetailInFaults = false,
                      UseSynchronizationContext = true)]
    public class DeploymentCacheService : IDeploymentCacheOperations
    {
        private ContentCache contentCache = new ContentCache(@"C:\Cache", 300);

        public DeploymentCacheResponse GetZipFileForSite(DeploymentCacheRequest cacheRequest)
        {
            DeploymentCacheResponse cacheResponse = new DeploymentCacheResponse();

            cacheResponse.SiteName = cacheRequest.SiteName;

            cacheResponse.FileContents = contentCache.AddSite(cacheRequest.RootDirectory, cacheRequest.StorageVolumePath);

            if (cacheResponse.FileContents == null)
            {
                // Failed to add site to Cache
                DeploymentCacheFault df = new DeploymentCacheFault("Failed to add site to Cache. Could not get site content", cacheRequest.RootDirectory, cacheRequest.StorageVolumePath);
                throw new FaultException<DeploymentCacheFault>(df);
            }

            cacheResponse.FileLength = cacheResponse.FileContents.Length;

            return cacheResponse;
        }

        public DeploymentCacheResponse RefreshCacheForSite(DeploymentCacheRequest cacheRequest)
        {
            DeploymentCacheResponse cacheRefreshResponse = new DeploymentCacheResponse();

            cacheRefreshResponse.SiteName = cacheRequest.SiteName;

            cacheRefreshResponse.FileContents = contentCache.UpdateSite(cacheRequest.RootDirectory, cacheRequest.StorageVolumePath);

            if (cacheRefreshResponse.FileContents == null)
            {
                // Failed to add site to Cache
                DeploymentCacheFault df = new DeploymentCacheFault("Failed to Refersh site in Cache. Could not get site contents", cacheRequest.RootDirectory, cacheRequest.StorageVolumePath);
                throw new FaultException<DeploymentCacheFault>(df);
            }

            cacheRefreshResponse.FileLength = cacheRefreshResponse.FileContents.Length;

            return cacheRefreshResponse;
        }

        public DeleteFromCacheResponse DeleteCacheForSite(DeploymentCacheRequest cacheRequest)
        {
            DeleteFromCacheResponse deleteFromCacheResponse = new DeleteFromCacheResponse();

            try
            {
                contentCache.DropSite(cacheRequest.RootDirectory);
            }
            catch (Exception)
            {
                DeploymentCacheFault df = new DeploymentCacheFault("Failed to delete site from Cache.", cacheRequest.RootDirectory, cacheRequest.StorageVolumePath);
                throw new FaultException<DeploymentCacheFault>(df);
            }

            deleteFromCacheResponse.IsDeleteSuccessful = true;

            return deleteFromCacheResponse;
        }

        public void ClearCache()
        {
            try
            {
                contentCache.ClearCache();
            }
            catch (Exception)
            {
                DeploymentCacheFault df = new DeploymentCacheFault("Failed to Clear Cache contents.");
                throw new FaultException<DeploymentCacheFault>(df);
            }
        }

        public DeploymentCacheStats GetDeploymentCacheStats()
        {
            DeploymentCacheStats deploymentCacheStats = new DeploymentCacheStats();
            try
            {
                CacheStats cacheStatsFromStorage = contentCache.GetCacheStats();

                deploymentCacheStats.NumberOfSitesInCache = cacheStatsFromStorage.NumberOfSitesInCache;
                deploymentCacheStats.CacheCapacityBytes = cacheStatsFromStorage.CacheCapacityBytes;
                deploymentCacheStats.CacheFreeSpaceBytes = cacheStatsFromStorage.CacheFreeSpaceBytes;
                deploymentCacheStats.CacheUsedSpaceBytes = cacheStatsFromStorage.CacheUsedSpaceBytes;
            }
            catch(Exception)
            {
                DeploymentCacheFault df = new DeploymentCacheFault("Failed to get Deployment Cache stats.");
                throw new FaultException<DeploymentCacheFault>(df);
            }

            return deploymentCacheStats;
        }

        public DeploymentCacheResponse TestDowloadSpeedForZip(DeploymentCacheRequest cacheRequest)
        {
            DeploymentCacheResponse cacheResponse = new DeploymentCacheResponse();

            cacheResponse.FileName = cacheRequest.RootDirectory;

            string fileAbsolutePath = Path.Combine(cacheRequest.StorageVolumePath, cacheRequest.RootDirectory); 
            
            try
            {
                cacheResponse.FileContents = File.ReadAllBytes(fileAbsolutePath);
                cacheResponse.FileLength = cacheResponse.FileContents.Length;
            }
            catch (Exception e)
            {
                DeploymentCacheFault df = new DeploymentCacheFault(e.Message);
                throw new FaultException<DeploymentCacheFault>(df);
            }

            return cacheResponse;
        }
    }
}
