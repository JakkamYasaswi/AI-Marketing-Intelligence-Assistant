
using System.Net;
using System.Net.Sockets;

namespace MarketingIntelligence.API.Services
{
    public class IpAddressValidator
    {
        public async Task<IPAddress[]> GetValidatedPublicAddressesAsync(
            string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
                throw new HttpRequestException("Invalid website URL.");

            if (uri.Scheme != Uri.UriSchemeHttp &&
                uri.Scheme != Uri.UriSchemeHttps)
                throw new HttpRequestException(
                    "Only HTTP and HTTPS URLs are allowed.");

            if (!string.IsNullOrEmpty(uri.UserInfo))
                throw new HttpRequestException(
                    "URL credentials are not allowed.");

            IPAddress[] addresses =
                await Dns.GetHostAddressesAsync(uri.DnsSafeHost);

            if (addresses.Length == 0)
                throw new HttpRequestException(
                    "Website IP address could not be found.");

            foreach (IPAddress address in addresses)
            {
                if (IsPrivateOrLocalIp(address))
                    throw new HttpRequestException(
                        "Access to private or local IP addresses is not allowed.");
            }

            return addresses;
        }

        private bool IsPrivateOrLocalIp(IPAddress ip)
        {
            if (ip.IsIPv4MappedToIPv6)
                ip = ip.MapToIPv4();

            if (IPAddress.IsLoopback(ip) ||
                ip.IsIPv6LinkLocal ||
                ip.IsIPv6SiteLocal ||
                ip.IsIPv6Multicast ||
                ip.Equals(IPAddress.Any) ||
                ip.Equals(IPAddress.IPv6Any))
                return true;

            if (ip.AddressFamily == AddressFamily.InterNetworkV6)
            {
                byte[] bytes = ip.GetAddressBytes();
                return (bytes[0] & 0xFE) == 0xFC;
            }

            if (ip.AddressFamily != AddressFamily.InterNetwork)
                return true;

            byte[] b = ip.GetAddressBytes();

            return b[0] == 0 ||
                   b[0] == 10 ||
                   b[0] == 127 ||
                   (b[0] == 100 && b[1] >= 64 && b[1] <= 127) ||
                   (b[0] == 169 && b[1] == 254) ||
                   (b[0] == 172 && b[1] >= 16 && b[1] <= 31) ||
                   (b[0] == 192 && b[1] == 168) ||
                   (b[0] == 192 && b[1] == 0) ||
                   (b[0] == 198 && (b[1] == 18 || b[1] == 19)) ||
                   (b[0] == 198 && b[1] == 51 && b[2] == 100) ||
                   (b[0] == 203 && b[1] == 0 && b[2] == 113) ||
                   b[0] >= 224;
        }
    }
}
