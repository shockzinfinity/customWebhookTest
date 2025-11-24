using System.Diagnostics;

namespace Webhooks.Processing.OpenTelemetry;

internal static class DiagnosticConfig
{
    internal static readonly ActivitySource Source = new("webhooks-processing");
}