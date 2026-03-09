using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CloudflaredMonitor.Services
{
    /// <summary>
    ///  Internal model capturing the aggregated status of the Windows service
    ///  and Cloudflare tunnel.  This model is used to drive the UI and
    ///  diagnostics exporter.
    /// </summary>
    internal sealed class TunnelServiceStatus
    {
        /// <summary>
        ///  Textual representation of the Windows service state.  Possible
        ///  values include "NotInstalled", "Stopped", "Running" and others
        ///  as returned by <see cref="System.ServiceProcess.ServiceController"/>.
        /// </summary>
        public string ServiceState { get; set; } = "Unknown";

        /// <summary>
        ///  GUID identifying the Cloudflare tunnel extracted from the
        ///  installation token.  Null if the tunnel could not be decoded.
        /// </summary>
        public string? TunnelId { get; set; }
;

        /// <summary>
        ///  Human friendly name of the tunnel, returned from the Cloudflare
        ///  API.  May be null if the API call fails.
        /// </summary>
        public string? TunnelName { get; set; }
;

        /// <summary>
        ///  Status of the tunnel as reported by Cloudflare.  Typical values
        ///  include "connected", "inactive" and "degraded".
        /// </summary>
        public string? RemoteStatus { get; set; }
;

        /// <summary>
        ///  Collection of ingress rules associated with this tunnel.  Each
        ///  entry describes a hostname/path to local service mapping.
        /// </summary>
        public List<IngressRuleView> Ingress { get; init; } = new();

        /// <summary>
        ///  Optional diagnostic note capturing any errors or warnings that
        ///  occurred while collecting status.  This is surfaced in the UI
        ///  and the log.
        /// </summary>
        public string? DiagnosticsNote { get; set; }
;
    }

    /// <summary>
    ///  View model representing a single ingress rule for display in the UI.
    /// </summary>
    internal sealed class IngressRuleView
    {
        public string Display { get; init; } = string.Empty;
    }

    // Below are models that mirror the structure returned by the Cloudflare API
    // for tunnel details, tokens and configurations.  JSON property names are
    // preserved as defined by the API.
    internal sealed class CfApiResponse<T>
    {
        [JsonPropertyName("success")] public bool Success { get; set; }
        [JsonPropertyName("errors")] public object[]? Errors { get; set; }
        [JsonPropertyName("messages")] public object[]? Messages { get; set; }
        [JsonPropertyName("result")] public T? Result { get; set; }
    }

    internal sealed class CfTunnel
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
    }

    internal sealed class CfTunnelTokenResult
    {
        [JsonPropertyName("token")] public string? Token { get; set; }
    }

    internal sealed class CfTunnelConfigWrapper
    {
        [JsonPropertyName("config")] public CfTunnelConfig? Config { get; set; }
    }

    internal sealed class CfTunnelConfig
    {
        [JsonPropertyName("ingress")] public List<CfIngressRule>? Ingress { get; set; }
    }

    internal sealed class CfIngressRule
    {
        [JsonPropertyName("hostname")] public string? Hostname { get; set; }
        [JsonPropertyName("path")] public string? Path { get; set; }
        [JsonPropertyName("service")] public string? Service { get; set; }
    }
}