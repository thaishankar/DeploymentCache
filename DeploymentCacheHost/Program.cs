using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;
using System.Threading.Tasks;
using DeploymentCacheLib;

namespace DeploymentCacheHost
{
    class Program
    {
        static void Main(string[] args)
        {
            // Step 1: Create a URI to serve as the base address.s
            Uri baseAddress = new Uri("http://localhost:8000/DeploymentCache/");

            // Step 2: Create a ServiceHost instance.
            ServiceHost selfHost = new ServiceHost(typeof(DeploymentCacheService), baseAddress);


            try
            {
                NetTcpBinding netTcpBinding = new NetTcpBinding();
                netTcpBinding.Security.Mode = SecurityMode.None;

                // Step 3: Add a service endpoint.
                selfHost.AddServiceEndpoint(typeof(IDeploymentCacheOperations), netTcpBinding, "net.tcp://localhost:8002/DeploymentCache/");

                // Step 4: Enable metadata exchange.
                ServiceMetadataBehavior smb = new ServiceMetadataBehavior();
                smb.HttpGetEnabled = true;
                smb.HttpGetEnabled = true;

                selfHost.Description.Behaviors.Add(smb);

                // Step 5: Start the service.
                selfHost.Open();
                Console.WriteLine("The service is ready.");

                // Close the ServiceHost to stop the service.
                Console.WriteLine("Press <Enter> to terminate the service.");
                Console.WriteLine();
                Console.ReadLine();
                selfHost.Close();
            }
            catch (CommunicationException ce)
            {
                Console.WriteLine("An exception occurred: {0}", ce.Message);
                selfHost.Abort();
            }
        }
    }
}
