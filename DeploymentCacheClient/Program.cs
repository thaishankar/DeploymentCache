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

namespace CacheClient
{
    class Program
    {
        static void Main(string[] args)
        {

            string clienIp;
            SUPPORTED_BINDINGS bindingToUse;
            string outFile = "1.zip";

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
                bindingToUse = (SUPPORTED_BINDINGS)Int32.Parse(args[1]);
            }
            else
            {
                bindingToUse = SUPPORTED_BINDINGS.NETTCP;
            }

            if(args.Length >= 3)
            {
                outFile = args[2];
            }

            DeploymentCacheOperationsClient cacheOperationsClient = null;
            EndpointAddress endpointAddress = CacheClientHelper.BuildEndPointAddress(clienIp, bindingToUse);

            Console.WriteLine("Endpoint {0}", endpointAddress);

            if (bindingToUse == SUPPORTED_BINDINGS.NETTCP)
            { 
                cacheOperationsClient = new DeploymentCacheOperationsClient(CacheClientHelper.ConfiguredNetTcpBinding(), endpointAddress);
            }
            else if (bindingToUse == SUPPORTED_BINDINGS.WSHTTP)
            {
                cacheOperationsClient = new DeploymentCacheOperationsClient(CacheClientHelper.ConfiguredWsHttpBinding(), endpointAddress);
            }

            if (cacheOperationsClient == null)
            {
                throw new Exception("Cache Client not configured. Aborting...");
            }

            DeploymentCacheRequest cacheRequest = new DeploymentCacheRequest();

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            DeploymentCacheResponse cacheResponse = cacheOperationsClient.GetZipFileForSite(cacheRequest);

            long downloadTime = stopWatch.ElapsedMilliseconds;

            Console.WriteLine(cacheResponse.FileName + " " + cacheResponse.FileLength);

            string outPath = outFile;
            File.WriteAllBytes(outPath, cacheResponse.FileContents);

            long endTime = stopWatch.ElapsedMilliseconds;

            Console.WriteLine("Download time = {0}ms, WriteTime: {1} TotalTime: {2}", downloadTime, endTime - downloadTime, endTime);
        }
    }
}
