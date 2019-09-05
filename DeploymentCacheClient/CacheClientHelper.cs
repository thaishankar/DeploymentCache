using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace CacheClient
{

    public enum SUPPORTED_BINDINGS : int
    {
        NETTCP = 0
    }

    public class Constants
    {
        public const int NETTCP_BINDING_PORT = 8002;   // TODO: Read the port from Hosting Config
        public const string DEPLOYMENT_CACHE_ENDPOINT = "DeploymentCache";
        public const string LOCAL_CLIENT_IP_ADDRESS = "localhost";
        public const int KILOBYTE = 1024;
        public const int MEGABYE = 1024 * KILOBYTE;
    }

    class CacheClientHelper
    {
        public static EndpointAddress BuildEndPointAddress(string ipAddress, SUPPORTED_BINDINGS bindingMode)
        {
            if (String.IsNullOrEmpty(ipAddress))
            {
                throw new ArgumentException("IP Address can not be null or empty");
            }

            string endpointAddress = null;

            if(bindingMode == SUPPORTED_BINDINGS.NETTCP)
            {
                endpointAddress = string.Format(@"net.tcp://{0}:{1}/{2}", ipAddress, Constants.NETTCP_BINDING_PORT, Constants.DEPLOYMENT_CACHE_ENDPOINT);
            }

            if(endpointAddress == null)
            {
                throw new ArgumentException(string.Format("Unsupported Binding Format: {0}", bindingMode));
            }

            return new EndpointAddress(endpointAddress);
        }

        public static NetTcpBinding ConfiguredNetTcpBinding()
        {
            NetTcpBinding netTcpBinding = new NetTcpBinding();

            netTcpBinding.MaxBufferPoolSize = 200000000;
            netTcpBinding.MaxReceivedMessageSize = 200 * Constants.MEGABYE;
            //netTcpBinding.MaxBufferSize = 200 * Constants.MEGABYE; // Not needed for TCP. Only used by streaming connections

            //netTcpBinding.Security.Mode = SecurityMode.Transport;
            //netTcpBinding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Certificate;
            netTcpBinding.ReaderQuotas.MaxDepth = 32;
            netTcpBinding.ReaderQuotas.MaxArrayLength = 200 * Constants.MEGABYE;
            netTcpBinding.ReaderQuotas.MaxBytesPerRead = 200 * Constants.MEGABYE;
            netTcpBinding.ReaderQuotas.MaxStringContentLength = 2 * Constants.MEGABYE;

            //netTcpBinding.CloseTimeout  // Default is 1 min.
            netTcpBinding.MaxConnections = 20;
            netTcpBinding.ListenBacklog = 20; // Max pending connections per endpoint before they are dropped
            //netTcpBinding.ReliableSession.Enabled = true;
            netTcpBinding.Security.Mode = SecurityMode.None;


            return netTcpBinding;
        }
    }
}
