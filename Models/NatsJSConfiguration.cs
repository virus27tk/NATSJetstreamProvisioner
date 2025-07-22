using System.Text.Json.Serialization;
using NATS.Client.JetStream;

namespace NATSJetstreamProvisioner.Models
{
    public class NatsJsConfigDefinition
    {
        [JsonPropertyName("natsUrl")]
        public string NatsUrl { get; set; } = "nats://localhost:4222"; // Default URL

        [JsonPropertyName("streams")]
        public List<StreamConfigDefinition> Streams { get; set; } = new List<StreamConfigDefinition>();
    }

    public class StreamConfigDefinition
    {
        [JsonPropertyName("contactDetails")]
        public string Contacts { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("subjects")]
        public List<string> Subjects { get; set; } = new List<string>();

        [JsonPropertyName("storageType")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public NATS.Client.JetStream.StorageType StorageType { get; set; } = StorageType.File;

        [JsonPropertyName("retentionPolicy")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public NATS.Client.JetStream.RetentionPolicy RetentionPolicy { get; set; } = RetentionPolicy.Limits;

        [JsonPropertyName("discardPolicy")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public NATS.Client.JetStream.DiscardPolicy DiscardPolicy { get; set; } = DiscardPolicy.Old;

        [JsonPropertyName("maxBytes")]
        public long MaxBytes { get; set; } = -1; // -1 means unlimited

        [JsonPropertyName("maxMessages")]
        public long MaxMessages { get; set; } = -1; // -1 means unlimited

        [JsonPropertyName("maxAgeHours")]
        public int MaxAgeHours { get; set; } = 0; // 0 means unlimited

        [JsonPropertyName("maxConsumers")]
        public int MaxConsumers { get; set; } = -1; // -1 means unlimited

        [JsonPropertyName("replicas")]
        public int Replicas { get; set; } = 1; // Default to 1 (no clustering)
    }
}
