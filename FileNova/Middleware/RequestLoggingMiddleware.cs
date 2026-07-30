using System.Diagnostics;

namespace FileNova.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();

            // Log de entrada
            _logger.LogInformation($"➡️ REQUEST INICIADA: {context.Request.Method} {context.Request.Path}");

            // Guardar el tiempo de inicio en el contexto para usarlo después
            context.Items[ "RequestStartTime" ] = stopwatch;

            await _next(context);

            stopwatch.Stop();

            // Log de salida con tiempo total
            _logger.LogInformation($"⬅️ REQUEST FINALIZADA: {context.Response.StatusCode} - Tiempo total: {stopwatch.ElapsedMilliseconds}ms");
        }
    }
}
