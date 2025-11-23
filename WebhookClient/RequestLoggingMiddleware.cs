using System.Text;

namespace WebhookClient;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public RequestLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        context.Request.EnableBuffering();

        using var reader = new StreamReader(
            context.Request.Body,
            encoding: Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            bufferSize: 1024,
            leaveOpen: true);

        string body = await reader.ReadToEndAsync();

        context.Request.Body.Position = 0;

        Console.WriteLine($"[REQUEST] Path: {context.Request.Path}, Body: {body}");

        await _next(context);
    }
}
