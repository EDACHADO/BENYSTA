
using System.Text.Json.Serialization;

namespace Integration.ViewModels;

public abstract class BaseAPIResponse
{
    [JsonPropertyName("isSuccess")]
    public bool IsSuccess => ResultType == ResultType.Success || ResultType == ResultType.Warning;

    [JsonPropertyName("message")] public string Message { get; set; }
    [JsonPropertyName("errors")] public List<string> Errors { get; set; }

    [JsonPropertyName("data")] public object Data { get; set; }

    [JsonPropertyName("resultType")] public ResultType ResultType { get; set; }
}
