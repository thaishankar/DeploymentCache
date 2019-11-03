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
        private ContentCache contentCache = new ContentCache(@"D:\Cache", 10);

        public DeploymentCacheResponse AddZipFileForSite(DeploymentCacheRequest cacheRequest)
        {
            DeploymentCacheResponse cacheResponse = new DeploymentCacheResponse();

            cacheResponse.SiteName = cacheRequest.SiteName;

            try
            {
                cacheResponse.FileContents = contentCache.AddSiteToCache(cacheRequest.SiteName, Path.Combine(cacheRequest.StorageVolumePath, cacheRequest.RootDirectory));
            }
            catch (Exception ex)
            {
                DeploymentCacheFault df = new DeploymentCacheFault(string.Format("Failed to add site in Cache. Ex: {0}", ex.ToString()),
                                                        cacheRequest.RootDirectory, cacheRequest.StorageVolumePath);
                throw new FaultException<DeploymentCacheFault>(df);
            }

            if (cacheResponse.FileContents == null)
            {
                // Failed to add site to Cache
                DeploymentCacheFault df = new DeploymentCacheFault("Failed to add site to Cache. Could not get site content", cacheRequest.RootDirectory, cacheRequest.StorageVolumePath);
                throw new FaultException<DeploymentCacheFault>(df);
            }

            cacheResponse.FileLength = cacheResponse.FileContents.Length;

            return cacheResponse;
        }

        public DeploymentCacheResponse AddZipFileForSiteWithUrl(DeploymentCacheRequest cacheRequest)
        {
            DeploymentCacheResponse cacheResponse = new DeploymentCacheResponse();

            cacheResponse.SiteName = cacheRequest.SiteName;

            cacheResponse.FileContents = contentCache.AddSiteWithUrlToCache(cacheRequest.SiteName, cacheRequest.RootDirectory);

            if (cacheResponse.FileContents == null)
            {
                // Failed to add site to Cache
                DeploymentCacheFault df = new DeploymentCacheFault("Failed to add site with URl");
                throw new FaultException<DeploymentCacheFault>(df);
            }

            cacheResponse.FileLength = cacheResponse.FileContents.Length;

            return cacheResponse;
        }

        public DeploymentCacheResponse GetSiteContents(DeploymentCacheRequest cacheRequest)
        {
            DeploymentCacheResponse cacheResponse = new DeploymentCacheResponse();

            cacheResponse.SiteName = cacheRequest.SiteName;

            try
            {
                cacheResponse.FileContents = contentCache.GetSiteContent(cacheRequest.SiteName, Path.Combine(cacheRequest.StorageVolumePath, cacheRequest.RootDirectory));
            }
            catch (Exception ex)
            {
                DeploymentCacheFault df = new DeploymentCacheFault(string.Format("Failed to GetSiteContents in Cache. Ex: {0}", ex.ToString()),
                                                        cacheRequest.RootDirectory, cacheRequest.StorageVolumePath);
                throw new FaultException<DeploymentCacheFault>(df);
            }

            if (cacheResponse.FileContents == null)
            {
                // Failed to add site to Cache
                DeploymentCacheFault df = new DeploymentCacheFault("Failed to get site content", cacheRequest.RootDirectory, cacheRequest.StorageVolumePath);
                throw new FaultException<DeploymentCacheFault>(df);
            }

            cacheResponse.FileLength = cacheResponse.FileContents.Length;

            return cacheResponse;
        }

        public DeploymentCacheResponse RefreshCacheForSite(DeploymentCacheRequest cacheRequest)
        {
            DeploymentCacheResponse cacheRefreshResponse = new DeploymentCacheResponse();

            cacheRefreshResponse.SiteName = cacheRequest.SiteName;

            try
            {
                cacheRefreshResponse.FileContents = contentCache.RefreshSiteContents(cacheRequest.SiteName, Path.Combine(cacheRequest.StorageVolumePath, cacheRequest.RootDirectory));
            }
            catch(Exception ex)
            {
                DeploymentCacheFault df = new DeploymentCacheFault(string.Format("Failed to Refersh site in Cache. Ex: {0}", ex.ToString()),
                                                        cacheRequest.RootDirectory, cacheRequest.StorageVolumePath);
                throw new FaultException<DeploymentCacheFault>(df);
            }

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
                contentCache.RemoveSiteFromCache(cacheRequest.SiteName);
            }
            catch (Exception e)
            {
                DeploymentCacheFault df = new DeploymentCacheFault(string.Format("Failed to delete site from Cache. Exception: {0}", e.ToString()),
                                                                    cacheRequest.RootDirectory, 
                                                                    cacheRequest.StorageVolumePath);
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
                //CacheStats cacheStatsFromStorage = contentCache.GetCacheStats();

                deploymentCacheStats.NumberOfSitesInCache = 0;
                deploymentCacheStats.CacheCapacityBytes = 0;
                deploymentCacheStats.CacheFreeSpaceBytes = 0;
                deploymentCacheStats.CacheUsedSpaceBytes = 0;
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
