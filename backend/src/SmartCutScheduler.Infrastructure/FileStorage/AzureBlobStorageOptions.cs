namespace SmartCutScheduler.Infrastructure.FileStorage;

public class AzureBlobStorageOptions
{
    public const string SectionName = "AzureStorage";

    public string ConnectionString { get; set; } = string.Empty;
    public string ProfileImagesContainer { get; set; } = "profile-images";
    public string FreshHaircutPhotosContainer { get; set; } = "fresh-haircut-photos";
}
