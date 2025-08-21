namespace KitePlatformManager.Configuration;

public class KitePlatformOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public int Port { get; set; }
    public string CertificatePath { get; set; } = string.Empty;
    public string CertificatePassword { get; set; } = string.Empty;
}
