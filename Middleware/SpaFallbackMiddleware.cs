using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace ConduitPlcDemo.Middleware;

/// <summary>
/// Middleware para servir index.html para rutas de SPA, pero NO para rutas de API o WebSocket
/// </summary>
public class SpaFallbackMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IFileProvider _fileProvider;
    private readonly ILogger<SpaFallbackMiddleware> _logger;

    public SpaFallbackMiddleware(
        RequestDelegate next,
        IFileProvider fileProvider,
        ILogger<SpaFallbackMiddleware> logger)
    {
        _next = next;
        _fileProvider = fileProvider;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Si se marcó para saltar el fallback, NO hacer nada
        if (context.Items.ContainsKey("SkipFallback") && (bool)context.Items["SkipFallback"]!)
        {
            await _next(context);
            return;
        }

        // Si es una ruta de API o WebSocket, NO hacer nada (dejar que pase al siguiente middleware)
        if (context.Request.Path.StartsWithSegments("/api") || 
            context.Request.Path.StartsWithSegments("/ws") ||
            context.Request.Path.StartsWithSegments("/swagger"))
        {
            await _next(context);
            return;
        }

        // Si el archivo existe, servirlo normalmente
        var fileInfo = _fileProvider.GetFileInfo(context.Request.Path.Value ?? "/");
        if (fileInfo.Exists && !fileInfo.IsDirectory)
        {
            await _next(context);
            return;
        }

        // Si no existe y no es una ruta de API/WebSocket, servir index.html (SPA fallback)
        var indexFile = _fileProvider.GetFileInfo("/index.html");
        if (indexFile.Exists)
        {
            context.Request.Path = "/index.html";
            await _next(context);
        }
        else
        {
            // Si index.html no existe, continuar normalmente (devolverá 404)
            await _next(context);
        }
    }
}

