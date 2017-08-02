using System.Net;
using Wikiled.Core.Utility.Extensions;

namespace Wikiled.Redis.Helpers
{
    public static class NetworkHelper
    {
        public static string GetAddress(this EndPoint address)
        {
            var ipEndPoint = address as IPEndPoint;
            if (ipEndPoint != null)
            {
                return $"{ipEndPoint.Address.MapToIPv4()}:{ipEndPoint.Port}";
            }

            var dnsEndPoint = address as DnsEndPoint;
            if (dnsEndPoint != null)
            {
                return $"{dnsEndPoint.Host.ToIpAddress()}:{dnsEndPoint.Port}";
            }

            return address.ToString();
        }
    }
}
