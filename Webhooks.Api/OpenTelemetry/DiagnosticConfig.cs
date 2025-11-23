using System.Diagnostics;

namespace Webhooks.Api.OpenTelemetry;

internal static class DiagnosticConfig
{
    internal static readonly ActivitySource Source = new("webhooks-api");
}