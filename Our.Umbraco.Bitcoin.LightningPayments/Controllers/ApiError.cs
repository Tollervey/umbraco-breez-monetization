namespace Our.Umbraco.Bitcoin.LightningPayments.Controllers
{
 /// <summary>
 /// Standard error envelope returned by the API.
 /// </summary>
 public class ApiError 
 { 
 /// <summary>
 /// Machine-readable error code.
 /// </summary>
 public string error { get; set; } = string.Empty; 
 /// <summary>
 /// Human-readable error message.
 /// </summary>
 public string message { get; set; } = string.Empty; 
 }
}

