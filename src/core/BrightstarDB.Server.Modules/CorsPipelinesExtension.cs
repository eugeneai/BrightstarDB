using System;
using System.Linq;
using BrightstarDB.Client;
using BrightstarDB.Dto;
using BrightstarDB.Server.Modules.Configuration;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Configuration;
using Nancy.Responses;
using Nancy.Responses.Negotiation;

namespace BrightstarDB.Server.Modules
{
    public static class CorsPipelinesExtension
    {
        public static void EnableCors(this IPipelines pipelines, CorsConfiguration corsConfiguration, INancyEnvironment environment)
        {
            pipelines.AfterRequest.AddItemToEndOfPipeline(ctx =>
            {
                UpdateResponseHeaders(ctx.Request, ctx.Response, corsConfiguration);
            });

            pipelines.OnError.AddItemToEndOfPipeline((ctx, exception) =>
            {
                if (exception == null)
                {
                    // Nothing to serialize, just return default 500 response
                    return HttpStatusCode.InternalServerError;
                }
                Response response;
                if (ctx.Request.Headers.Accept.Any(x => x.Item1.ToLowerInvariant().Contains("application/json")))
                {
                    // Return the exception detail as JSON
                    response = new JsonResponse(new ExceptionDetailObject(exception), 
                        new DefaultJsonSerializer(environment), environment) {StatusCode = HttpStatusCode.InternalServerError};
                }
                else
                {
                    // Return the exception message as text/plain
                    response = new TextResponse(HttpStatusCode.InternalServerError, exception.Message);
                }
                UpdateResponseHeaders(ctx.Request, response, corsConfiguration);
                return response;
            });
        }

        private static void UpdateResponseHeaders(Request request, Response response, CorsConfiguration corsConfiguration)
        {
            if (!request.Headers.Keys.Contains("Origin")) return;
            response.WithHeader("Access-Control-Allow-Origin", corsConfiguration.AllowOrigin);
            if (request.Method.Equals("OPTIONS"))
            {
                response
                    .WithHeader("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, PATCH, OPTIONS")
                    .WithHeader("Access-Control-Allow-Headers", "Accept, Origin, Content-type");
            }
        }
    }
}
