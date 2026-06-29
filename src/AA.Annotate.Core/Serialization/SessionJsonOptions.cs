using System.Text.Json;
using System.Text.Json.Serialization;

namespace AA.Annotate.Core.Serialization;

public static class SessionJsonOptions
{
    public static JsonSerializerOptions Create()
    {
        return new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };
    }
}
