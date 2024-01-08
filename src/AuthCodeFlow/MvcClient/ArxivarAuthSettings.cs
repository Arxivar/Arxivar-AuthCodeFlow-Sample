namespace MvcClient;

public class ArxivarAuthSettings
{
    /// <summary>
    /// Gets or Sets the url of the Auth service
    /// </summary>
    public string AuthServiceBaseUrl { get; set; }

    /// <summary>
    /// Gets or sets the client id
    /// </summary>
    public string ClientId { get; set; }

    /// <summary>
    /// Gets or sets the client secret
    /// </summary>
    public string ClientSecret { get; set; }
}