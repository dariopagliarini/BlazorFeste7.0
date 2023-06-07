using Newtonsoft.Json;

namespace BlazorFeste.Classes
{
  public class AnagrDataChange
  {
    [JsonProperty("key")]
    public int Key { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("data")]
    public object Data { get; set; }
  }
}
