using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace DeploymentCacheLib
{
    public class DeploymentCacheService : IDeploymentCacheOperations
    {
        public DeploymentCacheResponse GetZipFileForSite(DeploymentCacheRequest cacheRequest)
        {
            DeploymentCacheResponse cacheResponse = new DeploymentCacheResponse();

            cacheResponse.FileName = @"D:\TestZip\File10mb_0.zip";
            //cacheResponse.FileName = @"C:\Users\thshanmu\Desktop\Team\Run-From-Zip\Hello.zip";

            cacheResponse.FileContents = File.ReadAllBytes(cacheResponse.FileName);
            cacheResponse.FileLength = cacheResponse.FileContents.Length;

            return cacheResponse;
        }

        public CacheRefreshResponse RefreshCacheForSite(DeploymentCacheRequest cacheRequest)
        {
            CacheRefreshResponse refreshResponse = new CacheRefreshResponse();

            refreshResponse.FileName = "refreshResponse";
            refreshResponse.FileLength = 10;

            return refreshResponse;
        }

    }
}
