using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using DeploymentCacheClient.ServiceReference1;
using StorageCacheLib;

namespace CacheClient
{
    class Program
    {
        static void Main(string[] args)
        {

            string clienIp;
            string outFile = "OutFile.zip";

            if (args.Length >= 1)
            {
                clienIp = args[0];
            }
            else
            {
                clienIp = Constants.CLIENT_IP_ADDRESS;
            }

            if (args.Length >= 2)
            {
                outFile = args[1];
            }

            DeploymentCacheOperationsClient cacheOperationsClient = null;
            EndpointAddress endpointAddress = CacheClientHelper.BuildEndPointAddress(clienIp, SUPPORTED_BINDINGS.NETTCP);

            Console.WriteLine("Endpoint {0}", endpointAddress);

            cacheOperationsClient = new DeploymentCacheOperationsClient(CacheClientHelper.ConfiguredNetTcpBinding(), endpointAddress);

            if (cacheOperationsClient == null)
            {
                throw new Exception("Cache Client not configured. Aborting...");
            }

            DeploymentCacheRequest cacheRequest = new DeploymentCacheRequest();

            //cacheRequest.SiteName = "testSite";
            //cacheRequest.StorageVolumePath = @"D:\";
            //cacheRequest.RootDirectory = "home";

            cacheRequest.SiteName = "thsite";
            cacheRequest.StorageVolumePath = @"\\10.218.0.7\volume-4-default";
            cacheRequest.RootDirectory = @"8ae6c87deafffb4cbfef\b9eab18c5e654fce8b543e554ca56891";

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            int count = 5;
            try
            {
                while (true)
                {
                    DeploymentCacheResponse cacheResponse = cacheOperationsClient.GetZipFileForSite(cacheRequest);

                    Console.WriteLine("Response: File: {0} Size: {1}", cacheResponse.FileName, cacheResponse.FileContents.Length);

                    cacheResponse = cacheOperationsClient.GetZipFileForSite(cacheRequest);

                    Console.WriteLine("Response: File: {0} Size: {1}", cacheResponse.FileName, cacheResponse.FileContents.Length);

                    cacheResponse = cacheOperationsClient.RefreshCacheForSite(cacheRequest);

                    Console.WriteLine("Refresh Cache File: {0} Size: {1}", cacheResponse.FileName, cacheResponse.FileContents.Length);

                    DeploymentCacheStats deploymentCacheStats = cacheOperationsClient.GetDeploymentCacheStats();

                    Console.WriteLine("Stats: Sites: {0} Used: {1} Free: {2} Max: {3}",
                                            deploymentCacheStats.NumberOfSitesInCache,
                                            deploymentCacheStats.CacheUsedSpaceBytes,
                                            deploymentCacheStats.CacheFreeSpaceBytes,
                                            deploymentCacheStats.CacheCapacityBytes);

                    DeploymentCacheRequest deleteFromCacheRequest = new DeploymentCacheRequest();
                    deleteFromCacheRequest.RootDirectory = "home";
                    cacheOperationsClient.DeleteCacheForSite(deleteFromCacheRequest);
                }
            }
            catch (FaultException<DeploymentCacheFault> dcf)
            {
                Console.WriteLine("Cache Fault : SiteRoot: {0} Msg: {1} Stack: {2}", dcf.Detail.RootDirectory, dcf.Detail.Details, dcf.Detail.StackTrace);
            }
            catch (Exception e)
            {
                Console.WriteLine("Client Ex: {0}", e.ToString());
            }

            //long downloadTime = stopWatch.ElapsedMilliseconds;

            //Console.WriteLine(cacheResponse.FileName + " " + cacheResponse.FileLength);

            //string outPath = outFile;
            //File.WriteAllBytes(outPath, cacheResponse.FileContents);

            //long endTime = stopWatch.ElapsedMilliseconds;

            //Console.WriteLine("Download time = {0}ms, WriteTime: {1} TotalTime: {2}", downloadTime, endTime - downloadTime, endTime);
        }

        //static void Main(string[] args)
        //{

        //    DeploymentCacheRequest cacheRequest = new DeploymentCacheRequest();

        //    cacheRequest.SiteName = "thsite";
        //    cacheRequest.StorageVolumePath = @"D:\";
        //    cacheRequest.RootDirectory = @"home";

        //    //cacheRequest.StorageVolumePath = @"\\10.218.0.7\volume-4-default";
        //    //cacheRequest.RootDirectory = @"8ae6c87deafffb4cbfef\b9eab18c5e654fce8b543e554ca56891";

        //    Path.Combine(cacheRequest.StorageVolumePath, cacheRequest.RootDirectory);


        //    ContentCache contentCache = new ContentCache(@"D:\Cache", 3);

        //    contentCache.ClearCache();

        //    contentCache.AddSite(cacheRequest.RootDirectory, cacheRequest.StorageVolumePath);
        //    contentCache.UpdateSite(cacheRequest.RootDirectory, cacheRequest.StorageVolumePath);

        //    //contentCache.ClearCache();
        //}
    }
}
