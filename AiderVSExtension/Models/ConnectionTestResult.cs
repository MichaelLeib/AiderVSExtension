using System;
using System.Collections.Generic;

namespace AiderVSExtension.Models
{
    /// <summary>
    /// Represents the result of a connection test to an AI service provider
    /// </summary>
    public class ConnectionTestResult
    {
        /// <summary>
        /// Whether the connection test was successful
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Error message if the connection test failed
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Time taken to complete the connection test
        /// </summary>
        public TimeSpan ResponseTime { get; set; }

        /// <summary>
        /// Version of the model that was tested
        /// </summary>
        public string ModelVersion { get; set; }

        /// <summary>
        /// Additional information about the connection test
        /// </summary>
        public Dictionary<string, object> AdditionalInfo { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Timestamp when the test was performed
        /// </summary>
        public DateTime TestTimestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The provider that was tested (OpenAI, Claude, Ollama)
        /// </summary>
        public string Provider { get; set; }

        /// <summary>
        /// The endpoint URL that was tested
        /// </summary>
        public string EndpointUrl { get; set; }

        /// <summary>
        /// Whether the test involved authentication
        /// </summary>
        public bool RequiredAuthentication { get; set; }

        /// <summary>
        /// Initializes a new instance of the ConnectionTestResult class
        /// </summary>
        public ConnectionTestResult()
        {
        }

        /// <summary>
        /// Initializes a new instance of the ConnectionTestResult class with basic parameters
        /// </summary>
        /// <param name="isSuccessful">Whether the test was successful</param>
        /// <param name="errorMessage">Error message if failed</param>
        /// <param name="responseTime">Time taken for the test</param>
        public ConnectionTestResult(bool isSuccessful, string errorMessage = null, TimeSpan responseTime = default)
        {
            IsSuccessful = isSuccessful;
            ErrorMessage = errorMessage;
            ResponseTime = responseTime == default ? TimeSpan.Zero : responseTime;
        }

        /// <summary>
        /// Creates a successful connection test result
        /// </summary>
        /// <param name="responseTime">Time taken for the test</param>
        /// <param name="modelVersion">Version of the model tested</param>
        /// <returns>Successful connection test result</returns>
        public static ConnectionTestResult Success(TimeSpan responseTime, string modelVersion = null)
        {
            return new ConnectionTestResult(true, null, responseTime)
            {
                ModelVersion = modelVersion
            };
        }

        /// <summary>
        /// Creates a failed connection test result
        /// </summary>
        /// <param name="errorMessage">Error message describing the failure</param>
        /// <param name="responseTime">Time taken before failure</param>
        /// <returns>Failed connection test result</returns>
        public static ConnectionTestResult Failure(string errorMessage, TimeSpan responseTime = default)
        {
            return new ConnectionTestResult(false, errorMessage, responseTime);
        }

        /// <summary>
        /// Returns a string representation of the connection test result
        /// </summary>
        /// <returns>String representation</returns>
        public override string ToString()
        {
            if (IsSuccessful)
            {
                return $"Connection test successful - Response time: {ResponseTime.TotalMilliseconds}ms" +
                       (string.IsNullOrEmpty(ModelVersion) ? "" : $", Model: {ModelVersion}");
            }
            else
            {
                return $"Connection test failed - {ErrorMessage}" +
                       (ResponseTime > TimeSpan.Zero ? $" (after {ResponseTime.TotalMilliseconds}ms)" : "");
            }
        }
    }
}