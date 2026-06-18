namespace Backend.Api;

using System.Text.Json;
using System.Text.Json.Serialization;

internal static class SseJson
{
	public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
	{
		Converters = { new JsonStringEnumConverter() },
	};
}
