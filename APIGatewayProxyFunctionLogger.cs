using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Newtonsoft.Json;
using Serilog;

namespace MhLabs.APIGatewayLambdaProxy.Logging
{
    public static class APIGatewayProxyFunctionLogger
    {
        /// <summary>
        /// Used to wrap the FunctionHandlerAsync-call with added logging for request and response via Serilog
        /// </summary>
        /// <param name="request"></param>
        /// <param name="lambdaContext"></param>
        /// <param name="func"></param>
        /// <param name="logger"></param>
        /// <param name="logApiGatewayProxyResponse">IMPORTANT: Response body can be huge in size and logging it can have impact on performance and stability. Know what you are doing before you enable this.</param>
        /// <returns></returns>
        public static async Task<APIGatewayProxyResponse> LogFunctionHandlerAsync(APIGatewayProxyRequest request, ILambdaContext lambdaContext, Func<APIGatewayProxyRequest, ILambdaContext, Task<APIGatewayProxyResponse>> func, ILogger logger, bool logApiGatewayProxyResponse = false)
        {
            var context = logger.ForContext(typeof(APIGatewayProxyFunctionLogger));
            context.Information("Request - {Method} - {Path}", request.HttpMethod, request.Path);
            context.Information("ProxyRequest: {@Request}", request);
            context.Information("Context: {@ILambdaContext}", lambdaContext);
            context.Information("Claims: {@Claims}", request.RequestContext?.Authorizer?.Claims);

            // Invoke func
            var start = Stopwatch.GetTimestamp();
            var response = await func(request, lambdaContext);
            var elapsed = GetElapsedMilliseconds(start, Stopwatch.GetTimestamp());

            if (response.StatusCode >= 400)
            {
                context.Error("Response - {Method} - {Path} responded {StatusCode} in {Elapsed:0.0000} ms", request.HttpMethod, request.Path, response.StatusCode, elapsed);
            }
            else
            {
                context.Information("Response - {Method} - {Path} responded {StatusCode} in {Elapsed:0.0000} ms", request.HttpMethod, request.Path, response.StatusCode, elapsed);
            }

            if (logApiGatewayProxyResponse)
            {
                context.Information("ProxyResponse: {@Response}", response); 
            }
            
            return response;
        }

        private static double GetElapsedMilliseconds(long start, long stop)
        {
            return (stop - start) * 1000 / (double)Stopwatch.Frequency;
        }
    }
}