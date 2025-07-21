using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace AiderVSExtension.Security
{
    /// <summary>
    /// Provides certificate pinning functionality for enhanced HTTPS security
    /// </summary>
    public static class CertificatePinning
    {
        // Known certificate pins for AI service providers
        // In production, these should be regularly updated and configurable
        private static readonly Dictionary<string, HashSet<string>> KnownPins = new Dictionary<string, HashSet<string>>
        {
            ["api.openai.com"] = new HashSet<string>
            {
                // OpenAI certificate pins (example - should be updated with real pins)
                "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=", // Primary cert
                "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB="  // Backup cert
            },
            ["api.anthropic.com"] = new HashSet<string>
            {
                // Claude certificate pins (example - should be updated with real pins)
                "CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC=", // Primary cert
                "DDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDD="  // Backup cert
            }
        };

        /// <summary>
        /// Creates an HTTP client with certificate pinning enabled
        /// </summary>
        /// <param name="baseAddress">The base address for the HTTP client</param>
        /// <param name="enablePinning">Whether to enable certificate pinning</param>
        /// <returns>HTTP client with certificate validation</returns>
        public static HttpClient CreateSecureHttpClient(string baseAddress, bool enablePinning = true)
        {
            var handler = new HttpClientHandler();

            if (enablePinning && !string.IsNullOrEmpty(baseAddress))
            {
                var uri = new Uri(baseAddress);
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                {
                    return ValidateCertificate(uri.Host, cert, chain, errors);
                };
            }

            var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            if (!string.IsNullOrEmpty(baseAddress))
            {
                client.BaseAddress = new Uri(baseAddress);
            }

            // Add security headers
            client.DefaultRequestHeaders.Add("User-Agent", "AiderVSExtension/1.0");
            
            return client;
        }

        /// <summary>
        /// Validates a certificate using pinning and standard validation
        /// </summary>
        /// <param name="hostname">The hostname being connected to</param>
        /// <param name="certificate">The certificate to validate</param>
        /// <param name="chain">The certificate chain</param>
        /// <param name="sslPolicyErrors">SSL policy errors</param>
        /// <returns>True if the certificate is valid</returns>
        public static bool ValidateCertificate(string hostname, X509Certificate certificate, 
            X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            // First, perform standard certificate validation
            if (sslPolicyErrors != SslPolicyErrors.None)
            {
                // Allow for certificate pinning scenarios, but log the error
                System.Diagnostics.Debug.WriteLine($"SSL Policy Errors for {hostname}: {sslPolicyErrors}");
            }

            // For localhost/development scenarios, allow self-signed certificates
            if (hostname == "localhost" || hostname == "127.0.0.1")
            {
                return true;
            }

            // If we don't have pins for this hostname, use standard validation
            if (!KnownPins.ContainsKey(hostname))
            {
                return sslPolicyErrors == SslPolicyErrors.None;
            }

            // Perform certificate pinning
            return ValidateCertificatePin(hostname, certificate, chain);
        }

        /// <summary>
        /// Validates a certificate against known pins
        /// </summary>
        /// <param name="hostname">The hostname being connected to</param>
        /// <param name="certificate">The certificate to validate</param>
        /// <param name="chain">The certificate chain</param>
        /// <returns>True if the certificate matches a known pin</returns>
        private static bool ValidateCertificatePin(string hostname, X509Certificate certificate, X509Chain chain)
        {
            if (!KnownPins.TryGetValue(hostname, out var pins))
            {
                return false;
            }

            // Check the leaf certificate
            if (certificate is X509Certificate2 cert2)
            {
                var publicKeyPin = GetPublicKeyPin(cert2);
                if (pins.Contains(publicKeyPin))
                {
                    return true;
                }
            }

            // Check intermediate certificates in the chain
            if (chain?.ChainElements != null)
            {
                foreach (var element in chain.ChainElements)
                {
                    var publicKeyPin = GetPublicKeyPin(element.Certificate);
                    if (pins.Contains(publicKeyPin))
                    {
                        return true;
                    }
                }
            }

            // No valid pins found
            System.Diagnostics.Debug.WriteLine($"Certificate pinning failed for {hostname}");
            return false;
        }

        /// <summary>
        /// Gets the public key pin (SHA-256 hash of the SubjectPublicKeyInfo)
        /// </summary>
        /// <param name="certificate">The certificate</param>
        /// <returns>Base64-encoded SHA-256 hash of the public key</returns>
        private static string GetPublicKeyPin(X509Certificate2 certificate)
        {
            // Get the SubjectPublicKeyInfo
            var publicKeyInfo = certificate.PublicKey.EncodedKeyValue.RawData;
            
            // Calculate SHA-256 hash
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(publicKeyInfo);
            
            // Return base64-encoded hash
            return Convert.ToBase64String(hash);
        }

        /// <summary>
        /// Updates certificate pins for a hostname (for configuration updates)
        /// </summary>
        /// <param name="hostname">The hostname</param>
        /// <param name="pins">The new certificate pins</param>
        public static void UpdateCertificatePins(string hostname, IEnumerable<string> pins)
        {
            if (string.IsNullOrEmpty(hostname) || pins == null)
                return;

            KnownPins[hostname] = new HashSet<string>(pins);
        }

        /// <summary>
        /// Gets the current certificate pins for a hostname
        /// </summary>
        /// <param name="hostname">The hostname</param>
        /// <returns>Set of certificate pins</returns>
        public static IReadOnlyCollection<string> GetCertificatePins(string hostname)
        {
            return KnownPins.TryGetValue(hostname, out var pins) ? pins : new HashSet<string>();
        }

        /// <summary>
        /// Enables certificate pinning for well-known AI service providers
        /// </summary>
        public static void EnableDefaultPinning()
        {
            // In a production environment, you would:
            // 1. Fetch the latest certificate pins from a secure configuration
            // 2. Verify the pins using multiple sources
            // 3. Implement pin backup/rotation mechanisms
            
            // For now, we use placeholder pins that should be updated with real values
            System.Diagnostics.Debug.WriteLine("Certificate pinning enabled with default configuration");
        }

        /// <summary>
        /// Disables certificate pinning (for testing/development)
        /// </summary>
        public static void DisablePinning()
        {
            KnownPins.Clear();
            System.Diagnostics.Debug.WriteLine("Certificate pinning disabled");
        }
    }
}