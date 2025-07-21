using System;
using System.Text.RegularExpressions;

namespace AiderVSExtension.Security
{
    /// <summary>
    /// Provides secure URL building with HTTPS enforcement where appropriate
    /// </summary>
    public static class SecureUrlBuilder
    {
        private static readonly Regex LocalhostPattern = new Regex(
            @"^(localhost|127\.0\.0\.1|::1)$", 
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Builds a secure URL with HTTPS enforcement for non-localhost addresses
        /// </summary>
        /// <param name="host">The hostname</param>
        /// <param name="port">The port number</param>
        /// <param name="path">Optional path component</param>
        /// <returns>Secure URL with appropriate protocol</returns>
        public static string BuildSecureUrl(string host, int port, string path = null)
        {
            if (string.IsNullOrEmpty(host))
                throw new ArgumentException("Host cannot be null or empty", nameof(host));

            if (port < 1 || port > 65535)
                throw new ArgumentException("Port must be between 1 and 65535", nameof(port));

            // Use HTTP only for localhost/loopback addresses
            var protocol = IsLocalhost(host) ? "http" : "https";
            
            var url = $"{protocol}://{host}:{port}";
            
            if (!string.IsNullOrEmpty(path))
            {
                if (!path.StartsWith("/"))
                    path = "/" + path;
                url += path;
            }

            return url;
        }

        /// <summary>
        /// Enforces HTTPS for a given URL unless it's a localhost address
        /// </summary>
        /// <param name="url">The URL to secure</param>
        /// <returns>URL with HTTPS protocol enforced where appropriate</returns>
        public static string EnforceHttps(string url)
        {
            if (string.IsNullOrEmpty(url))
                return url;

            try
            {
                var uri = new Uri(url);
                
                // Allow HTTP for localhost/loopback addresses
                if (IsLocalhost(uri.Host))
                {
                    return url;
                }

                // Enforce HTTPS for non-localhost addresses
                if (uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase))
                {
                    var builder = new UriBuilder(uri)
                    {
                        Scheme = "https",
                        Port = uri.Port == 80 ? 443 : uri.Port
                    };
                    return builder.ToString();
                }

                return url;
            }
            catch (UriFormatException)
            {
                // Return original URL if parsing fails
                return url;
            }
        }

        /// <summary>
        /// Validates if a URL uses secure protocol or is localhost
        /// </summary>
        /// <param name="url">The URL to validate</param>
        /// <returns>True if the URL is secure or localhost</returns>
        public static bool IsSecureUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return false;

            try
            {
                var uri = new Uri(url);
                
                // HTTPS is always secure
                if (uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
                    return true;

                // HTTP is acceptable for localhost
                if (uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) && IsLocalhost(uri.Host))
                    return true;

                return false;
            }
            catch (UriFormatException)
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if a host is a localhost/loopback address
        /// </summary>
        /// <param name="host">The hostname to check</param>
        /// <returns>True if the host is localhost/loopback</returns>
        public static bool IsLocalhost(string host)
        {
            if (string.IsNullOrEmpty(host))
                return false;

            return LocalhostPattern.IsMatch(host);
        }

        /// <summary>
        /// Gets the default secure port for a protocol
        /// </summary>
        /// <param name="protocol">The protocol (http/https)</param>
        /// <returns>Default port number</returns>
        public static int GetDefaultPort(string protocol)
        {
            return protocol?.ToLowerInvariant() switch
            {
                "http" => 80,
                "https" => 443,
                _ => throw new ArgumentException($"Unsupported protocol: {protocol}")
            };
        }
    }
}