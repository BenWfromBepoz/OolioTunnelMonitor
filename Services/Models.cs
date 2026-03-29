using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OolioTunnelMonitor.Services
{
    internal sealed class TunnelServiceStatus
    {
        public string ServiceState { get; set; } = "Unknown";
        public string? TunnelId { get; set; }
        public string? TunnelName { get; set; }
        public string? RemoteStatus { get; set; }
        public List<IngressRuleView> Ingress { get; init; } = new();
        public string? DiagnosticsNote { get; set; }
    }

    internal sealed class IngressRuleView
    {
        public string Display { get; init; } = string.Empty;
    }

    internal sealed class CfApiResponse<T>
    {
        [JsonPropertyName("success")]  public bool Success { get; set; }
        [JsonPropertyName("errors")]   public object[]? Errors { get; set; }
        [JsonPropertyName("messages")] public object[]? Messages { get; set; }
        [JsonPropertyName("result")]   public T? Result { get; set; }
    }

    internal sealed class CfTunnel
    {
        [JsonPropertyName("id")]     public string? Id { get; set; }
        [JsonPropertyName("name")]   public string? Name { get; set; }
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
        [JsonPropertyName("path")]     public string? Path { get; set; }
        [JsonPropertyName("service")]  public string? Service { get; set; }
    }

    // Token verification - returned by /user/tokens/verify
    internal sealed class CfTokenVerifyResult
    {
        [JsonPropertyName("id")]       public string? Id { get; set; }
        [JsonPropertyName("status")]   public string? Status { get; set; }
        [JsonPropertyName("not_before")] public string? NotBefore { get; set; }
        [JsonPropertyName("expires_on")] public string? ExpiresOn { get; set; }
    }
}
