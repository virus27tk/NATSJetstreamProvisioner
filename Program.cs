using NATS.Client;
using NATS.Client.JetStream;
using NATSJetstreamProvisioner.Models;
using System.Text.Json;

namespace NatsInfrastructureProvisioner
{
    public class Program
    {
        private const string ConfigFileName = "nats_configuration.json";

        public static async Task Main(string[] args)
        {
            Console.WriteLine("Starting NATS JetStream Infrastructure Provisioner...");

            NatsJsConfigDefinition? config = null;
            try
            {
                // Read configuration from JSON file
                var json = await File.ReadAllTextAsync(ConfigFileName);
                config = JsonSerializer.Deserialize<NatsJsConfigDefinition>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true // Allows matching "natsUrl" or "NatsUrl"
                });

                if (config == null)
                {
                    Console.Error.WriteLine($"ERROR: Failed to deserialize {ConfigFileName}. Configuration is null.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(config.NatsUrl))
                {
                    Console.Error.WriteLine("ERROR: NATS URL is not specified in the configuration.");
                    return;
                }

                if (!config.Streams.Any())
                {
                    Console.WriteLine("No streams defined in the configuration. Exiting.");
                    return;
                }
            }
            catch (FileNotFoundException)
            {
                Console.Error.WriteLine($"ERROR: Configuration file '{ConfigFileName}' not found. Please ensure it's in the same directory as the executable.");
                return;
            }
            catch (JsonException ex)
            {
                Console.Error.WriteLine($"ERROR: Failed to parse JSON configuration: {ex.Message}");
                return;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ERROR: An unexpected error occurred while reading configuration: {ex.Message}");
                return;
            }

            // --- Connect to NATS and Provision JetStream ---
            var opts = ConnectionFactory.GetDefaultOptions();
            opts.Url = config.NatsUrl;

            using (var nc = new ConnectionFactory().CreateConnection(opts))
            {
                Console.WriteLine($"\nSuccessfully connected to NATS: {nc.ConnectedUrl}");
                //var js = nc.CreateJetStreamContext();

                IJetStream js = nc.CreateJetStreamContext();
                IJetStreamManagement jsm = nc.CreateJetStreamManagementContext();


                foreach (var streamDef in config.Streams)
                {
                    try
                    {
                        Console.WriteLine($"\nAttempting to provision stream: '{streamDef.Name}'...");

                        // Build the StreamConfiguration from the JSON definition
                        var streamConfigBuilder = StreamConfiguration.Builder()
                            .WithName(streamDef.Name)
                            .WithStorageType(streamDef.StorageType)
                            .WithRetentionPolicy(streamDef.RetentionPolicy)
                            .WithDiscardPolicy(streamDef.DiscardPolicy)
                            .WithReplicas(streamDef.Replicas);

                        // Add subjects
                        if (streamDef.Subjects != null && streamDef.Subjects.Any())
                        {
                            streamConfigBuilder.WithSubjects(streamDef.Subjects.ToArray());
                        }
                        else
                        {
                            Console.WriteLine($"WARNING: Stream '{streamDef.Name}' has no subjects defined. It will not capture messages.");
                        }

                        // Set limits if specified (non-zero or non-negative one)
                        if (streamDef.MaxBytes > 0)
                            streamConfigBuilder.WithMaxBytes(streamDef.MaxBytes);
                        if (streamDef.MaxMessages > 0)
                            streamConfigBuilder.WithMaxMessages((long)streamDef.MaxMessages);
                        if (streamDef.MaxAgeHours > 0)
                            streamConfigBuilder.WithMaxAge(NATS.Client.Internals.Duration.OfHours((long)streamDef.MaxAgeHours));
                        if (streamDef.MaxConsumers > 0)
                            streamConfigBuilder.WithMaxConsumers(streamDef.MaxConsumers);

                        var streamConfig = streamConfigBuilder.Build();

                        var streamInfo = jsm.AddStream(streamConfig);
                        Console.WriteLine($"Successfully provisioned stream: '{streamInfo.Config.Name}'");
                        Console.WriteLine($"  Subjects: {string.Join(", ", streamInfo.Config.Subjects)}");
                        Console.WriteLine($"  Storage Type: {streamInfo.Config.StorageType}");
                        Console.WriteLine($"  Retention Policy: {streamInfo.Config.RetentionPolicy}");
                        Console.WriteLine($"  Max Messages: {streamInfo.Config.MaxMsgs}");
                        Console.WriteLine($"  Max Bytes: {streamInfo.Config.MaxBytes}");
                        Console.WriteLine($"  Max Age: {streamInfo.Config.MaxAge}");
                        Console.WriteLine($"  Replicas: {streamInfo.Config.Replicas}");
                    }
                    catch (NATSJetStreamException ex)
                    {
                        Console.Error.WriteLine($"ERROR: JetStream specific error provisioning stream '{streamDef.Name}': {ex.Message}");
                        // Log more details if needed, e.g., ex.StackTrace
                    }
                    catch (NATSException ex)
                    {
                        Console.Error.WriteLine($"ERROR: NATS connection error while provisioning stream '{streamDef.Name}': {ex.Message}");
                        // This might indicate a lost connection to the NATS server.
                        break; // Exit the loop if connection is critical.
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"ERROR: An unexpected error occurred while provisioning stream '{streamDef.Name}': {ex.Message}");
                    }
                }
            }
            Console.WriteLine("\nNATS JetStream infrastructure provisioning complete.");
        }
    }
}