using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AuthenticationAPI.Utilities.NewFolder
{
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;

        public RequestResponseLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            // Get local IP address of the server
            var localIpAddress = GetLocalIpAddress();

            // Capture remote IP address
            var remoteIpAddress = context.Connection.RemoteIpAddress?.ToString();

            // Log request details
            LogToFile($"Request from: Remote IP address => {remoteIpAddress}, Local IP address =>  {localIpAddress}, Method Type => {context.Request.Method}, URL => {context.Request.Path}");

            // Capture request body if present
            if (context.Request.Body.CanSeek && context.Request.ContentLength > 0)
            {
                context.Request.Body.Seek(0, SeekOrigin.Begin);
                var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
                LogToFile($"Request Body: {requestBody}");
                context.Request.Body.Seek(0, SeekOrigin.Begin);
            }

            // Capture request headers
            var headers = new StringBuilder();
            foreach (var (key, value) in context.Request.Headers)
            {
                headers.AppendLine($"{key}: {value}");
            }

            // Add 21 spaces before each line
            var indentedHeaders = new StringBuilder();
            using (var reader = new StringReader(headers.ToString()))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    indentedHeaders.AppendLine(new string(' ', 20) + line);
                }
            }

            LogToFile($"Request Headers:\n{indentedHeaders}");


            // Capture response details
            var originalBodyStream = context.Response.Body;
            using (var responseBody = new MemoryStream())
            {
                context.Response.Body = responseBody;

                // Call the next middleware in the pipeline
                await _next(context);

                // Log response details
                LogToFile($"Response to: Remote IP address => {remoteIpAddress}, Local IP address =>  {localIpAddress}, Status Coode => {context.Response.StatusCode}");

                // Capture response body if present
                responseBody.Seek(0, SeekOrigin.Begin);
                var responseBodyContent = await new StreamReader(responseBody).ReadToEndAsync();
                LogToFile($"Response Body: {responseBodyContent}");

                // Copy the response body to the original stream
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
            }
        }
        private string GetLocalIpAddress()
        {
            var hostName = Dns.GetHostName();
            var hostEntry = Dns.GetHostEntry(hostName);
            foreach (var ipAddress in hostEntry.AddressList)
            {
                if (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return ipAddress.ToString();
                }
            }
            return null;
        }

        private void LogToFile(string message)
        {
            var folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Utilities", "ReqandResLogging");
            var filePath = Path.Combine(folderPath, "reqandres.txt");

            // Create the folder if it doesn't exist
            Directory.CreateDirectory(folderPath);

            // Append the log message to the file
            File.AppendAllText(filePath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}\n");
        }
    }
}
