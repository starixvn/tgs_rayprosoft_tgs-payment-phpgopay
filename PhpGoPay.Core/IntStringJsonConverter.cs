using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tgs.Payment.PhpGoPay;

public class IntStringJsonConverter : JsonConverter<string?>
{
	public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.Number)
			if (reader.TryGetDecimal(out var remark))
				return remark.ToString();

		return reader.GetString();
	}

	public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
		=> throw new NotImplementedException();
}