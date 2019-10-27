using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DeploymentCacheClient.ServiceReference1;
using StorageCacheLib;

namespace CacheClient
{
    class Program
    {
        //static void Main(string[] args)
        //{

        //    string serverIp;
        //    string outFile = "OutFile.zip";

        //    if (args.Length >= 1)
        //    {
        //        serverIp = args[0];
        //    }
        //    else
        //    {
        //        serverIp = Constants.CLIENT_IP_ADDRESS;
        //    }

        //    if (args.Length >= 2)
        //    {
        //        outFile = args[1];
        //    }

        //    DeploymentCacheOperationsClient cacheOperationsClient = null;
        //    EndpointAddress endpointAddress = CacheClientHelper.BuildEndPointAddress(serverIp, SUPPORTED_BINDINGS.NETTCP);

        //    Console.WriteLine("Endpoint {0}", endpointAddress);

        //    cacheOperationsClient = new DeploymentCacheOperationsClient(CacheClientHelper.ConfiguredNetTcpBinding(), endpointAddress);

        //    if (cacheOperationsClient == null)
        //    {
        //        throw new Exception("Cache Client not configured. Aborting...");
        //    }

        //    DeploymentCacheRequest cacheRequest = new DeploymentCacheRequest();

        //    //cacheRequest.SiteName = "testSite";
        //    //cacheRequest.StorageVolumePath = @"D:\";
        //    //cacheRequest.RootDirectory = "home";

        //    cacheRequest.SiteName = "thsite";
        //    cacheRequest.StorageVolumePath = @"\\10.218.0.7\volume-4-default";
        //    cacheRequest.RootDirectory = @"8ae6c87deafffb4cbfef\b9eab18c5e654fce8b543e554ca56891";

        //    Stopwatch stopWatch = new Stopwatch();
        //    stopWatch.Start();

        //    int count = 5;
        //    try
        //    {
        //        while (true)
        //        {
        //            DeploymentCacheResponse cacheResponse = cacheOperationsClient.GetZipFileForSite(cacheRequest);

        //            Console.WriteLine("Response: File: {0} Size: {1}", cacheResponse.FileName, cacheResponse.FileContents.Length);

        //            cacheResponse = cacheOperationsClient.GetZipFileForSite(cacheRequest);

        //            Console.WriteLine("Response: File: {0} Size: {1}", cacheResponse.FileName, cacheResponse.FileContents.Length);

        //            cacheResponse = cacheOperationsClient.RefreshCacheForSite(cacheRequest);

        //            Console.WriteLine("Refresh Cache File: {0} Size: {1}", cacheResponse.FileName, cacheResponse.FileContents.Length);

        //            DeploymentCacheStats deploymentCacheStats = cacheOperationsClient.GetDeploymentCacheStats();

        //            Console.WriteLine("Stats: Sites: {0} Used: {1} Free: {2} Max: {3}",
        //                                    deploymentCacheStats.NumberOfSitesInCache,
        //                                    deploymentCacheStats.CacheUsedSpaceBytes,
        //                                    deploymentCacheStats.CacheFreeSpaceBytes,
        //                                    deploymentCacheStats.CacheCapacityBytes);

        //            DeploymentCacheRequest deleteFromCacheRequest = new DeploymentCacheRequest();
        //            deleteFromCacheRequest.RootDirectory = "home";
        //            cacheOperationsClient.DeleteCacheForSite(deleteFromCacheRequest);
        //        }
        //    }
        //    catch (FaultException<DeploymentCacheFault> dcf)
        //    {
        //        Console.WriteLine("Cache Fault : SiteRoot: {0} Msg: {1} Stack: {2}", dcf.Detail.RootDirectory, dcf.Detail.Details, dcf.Detail.StackTrace);
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine("Client Ex: {0}", e.ToString());
        //    }

        //    //long downloadTime = stopWatch.ElapsedMilliseconds;

        //    //Console.WriteLine(cacheResponse.FileName + " " + cacheResponse.FileLength);

        //    //string outPath = outFile;
        //    //File.WriteAllBytes(outPath, cacheResponse.FileContents);

        //    //long endTime = stopWatch.ElapsedMilliseconds;

        //    //Console.WriteLine("Download time = {0}ms, WriteTime: {1} TotalTime: {2}", downloadTime, endTime - downloadTime, endTime);
        //}

        #region TestDownloadSpeed
        static void Main(string[] args)
        {

            string serverIp;
            string outFile = "OutFile.zip";

            if (args.Length >= 1)
            {
                serverIp = args[0];
            }
            else
            {
                serverIp = Constants.LOCAL_CLIENT_IP_ADDRESS;
            }

            if (args.Length >= 2)
            {
                outFile = args[1];
            }

            Random random = new Random();
            string statsFile = @"WCFNetTcpStats.csv";

            string headers = "FileName, FileNum, Size, DownloadTime\n";
            File.AppendAllText(statsFile, headers);

            DeploymentCacheRequest cacheRequest = new DeploymentCacheRequest();

            cacheRequest.SiteName = "thsite";
            cacheRequest.StorageVolumePath = @"D:\TestZip";

            Stopwatch watch = new Stopwatch();
            

            EndpointAddress endpointAddress = CacheClientHelper.BuildEndPointAddress(serverIp, SUPPORTED_BINDINGS.NETTCP);

            try
            {
                while (true)
                {
                    for (int i = 0; i <= 1; i++)
                    {
                        watch.Restart();

                        DeploymentCacheOperationsClient cacheOperationsClient = new DeploymentCacheOperationsClient(CacheClientHelper.ConfiguredNetTcpBinding(), endpointAddress);

                        int id = random.Next(10);
                        int size = (int)Math.Pow(10, i);

                        cacheRequest.RootDirectory = string.Format(@"File{0}mb_{1}.zip", size, id);

                        DeploymentCacheResponse cacheResponse = cacheOperationsClient.TestDowloadSpeedForZip(cacheRequest);

                        long downloadTime = watch.ElapsedMilliseconds;

                        File.WriteAllBytes(cacheRequest.RootDirectory, cacheResponse.FileContents);

                        long endTime = watch.ElapsedMilliseconds;

                        string stats = string.Format("{0}, {1}, {2}, {3}\n", cacheRequest.RootDirectory, id, size, watch.ElapsedMilliseconds);
                        File.AppendAllText(statsFile, stats);

                        cacheOperationsClient.Close();

                        Console.WriteLine("Download time = {0}ms, WriteTime: {1}ms TotalTime: {2}ms", downloadTime, endTime - downloadTime, endTime);

                        Thread.Sleep(1000);
                    }
                }
            }
            catch (FaultException<DeploymentCacheFault> dcf)
            {
                Console.WriteLine("Cache Fault : SiteRoot: {0} Msg: {1} Stack: {2}", dcf.Detail.RootDirectory, dcf.Detail.Details, dcf.Detail.StackTrace);
            }

        }
        #endregion

        #region TestStoraceCacheLib
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
        #endregion
    }
}
