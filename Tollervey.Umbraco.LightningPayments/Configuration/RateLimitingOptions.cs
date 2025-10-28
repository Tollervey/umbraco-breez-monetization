namespace Tollervey.Umbraco.LightningPayments.UI.Configuration
{
 /// <summary>
 /// Options to control rate limiting behavior for LightningPayments endpoints.
 /// These options are bindable from configuration under "LightningPayments:RateLimiting".
 /// </summary>
 public class RateLimitingOptions
 {
 /// <summary>
 /// Enable rate limiting features. Default: false
 /// </summary>
 public bool Enabled { get; set; } = false;

 /// <summary>
 /// Use the ASP.NET Core built-in RateLimiting middleware (AddRateLimiter / UseRateLimiter).
 /// If false, the package will still register an IRateLimiter implementation for in-code checks.
 /// Default: false
 /// </summary>
 public bool UseAspNetRateLimiter { get; set; } = false;

 /// <summary>
 /// Number of permits allowed per window (per partition key). Default:5
 /// </summary>
 public int PermitLimit { get; set; } =5;

 /// <summary>
 /// Window length in seconds for fixed-window limiting. Default:60
 /// </summary>
 public int WindowSeconds { get; set; } =60;

 /// <summary>
 /// How many requests can be queued while waiting for permits. Default:0
 /// </summary>
 public int QueueLimit { get; set; } =0;

 /// <summary>
 /// HTTP status code returned for rejected requests. Default:429
 /// </summary>
 public int RejectionStatusCode { get; set; } =429;

 /// <summary>
 /// If true, partition keys will be based on remote IP address; otherwise consumers must provide a bucket key.
 /// Default: true
 /// </summary>
 public bool PartitionByIp { get; set; } = true;
 }
}
