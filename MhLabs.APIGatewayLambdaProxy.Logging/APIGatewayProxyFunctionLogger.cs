﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Microsoft.Extensions.Logging;

namespace MhLabs.APIGatewayLambdaProxy.Logging
{
    public class APIGatewayProxyFunctionLogger
    {
        /// <summary>
        /// Used to wrap the FunctionHandlerAsync-call with added logging for request and response via Serilog
        /// </summary>
        /// <param name="request"></param>
        /// <param name="lambdaContext"></param>
        /// <param name="func"></param>
        /// <param name="logger"></param>
        /// <param name="logApiGatewayProxyResponse">IMPORTANT: Response body can be huge in size and logging it can have impact on performance and stability. Know what you are doing before you enable this.</param>
        /// <param name="logApiGatewayContext"></param>
        /// <returns></returns>
        public static async Task<APIGatewayProxyResponse> LogFunctionHandlerAsync(APIGatewayProxyRequest request, ILambdaContext lambdaContext, Func<APIGatewayProxyRequest, ILambdaContext, Task<APIGatewayProxyResponse>> func, ILogger<APIGatewayProxyFunctionLogger> logger, bool logApiGatewayProxyResponse = false, bool logApiGatewayContext = false, Action<APIGatewayProxyRequest, APIGatewayProxyResponse, double> postAction = null)
        {
            logger.LogInformation("Request - {Method} - {Path}", request.HttpMethod, request.Path);

            if (logApiGatewayContext)
            {
                logger.LogInformation("ProxyRequest: {@APIGatewayProxyRequest}. Context: {@ILambdaContext}. Claims: {@Claims}", request, lambdaContext, request.RequestContext?.Authorizer?.Claims);
            }

            // Invoke func
            var start = Stopwatch.GetTimestamp();
            var response = await func(request, lambdaContext);
            var elapsed = GetElapsedMilliseconds(start, Stopwatch.GetTimestamp());

            if (response.StatusCode >= 400)
            {
                logger.LogError("Response - {Method} - {Path} responded {StatusCode} in {Elapsed:0.0000} ms", request.HttpMethod, request.Path, response.StatusCode, (int) elapsed);
            }
            else
            {
                logger.LogInformation("Response - {Method} - {Path} responded {StatusCode} in {Elapsed:0.0000} ms", request.HttpMethod, request.Path, response.StatusCode, (int) elapsed);
            }

            if (logApiGatewayProxyResponse)
            {
                logger.LogInformation("ProxyResponse: {@APIGatewayProxyResponse}", response);
            }

            postAction?.Invoke(request, response, elapsed);

            return response;
        }

        private static double GetElapsedMilliseconds(long start, long stop)
        {
            return (stop - start) * 1000 / (double)Stopwatch.Frequency;
        }
    }
}