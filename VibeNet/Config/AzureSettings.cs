namespace VibeNet.Config
{
    public class AzureSettings
    {
        public string SqlDatabase { get; set; } = String.Empty;
        public string CosmosDb { get; set;} = String.Empty;
        public string BlobConnectionString { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public string ImagesContainer { get; set; } = "images";
    }
}
