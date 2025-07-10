using System.Text.Json.Serialization;

namespace CourtSpotter.Infrastructure.BookingProviders.Playtomic.Sync;

public class PlaytomicCourtsJsonResponse
{
    [JsonPropertyName("props")]
    public Props Props { get; set; }
}

public class Props
{
    [JsonPropertyName("pageProps")]
    public PageProps PageProps { get; set; }
}


public class PageProps
{
    [JsonPropertyName("tenant")]
    public Tenant Tenant { get; set; }
}

public class Tenant
{
    [JsonPropertyName("tenant_id")]
    public string TenantId { get; set; }
    
    [JsonPropertyName("tenant_name")]
    public string TenantName { get; set; }
    
    [JsonPropertyName("resources")]
    public List<Resource> Resources { get; set; }
}

public class Resource
{
    [JsonPropertyName("resource_id")]
    public string ResourceId { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("properties")]
    public ResourceProperties Properties { get; set; }
}

public class ResourceProperties
{
    [JsonPropertyName("resource_type")]
    public string ResourceType { get; set; }
    
    [JsonPropertyName("resource_size")]
    public string ResourceSize { get; set; }
    
    [JsonPropertyName("resource_feature")]
    public string ResourceFeature { get; set; }
}

