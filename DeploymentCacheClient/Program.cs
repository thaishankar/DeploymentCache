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
            string outFile = "OutFile.zip";

            if (args.Length >= 1)
            {
                clienIp = args[0];
            }
            else
            {
                clienIp = Constants.CLIENT_IP_ADDRESS;
            }

            if(args.Length >= 2)
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
