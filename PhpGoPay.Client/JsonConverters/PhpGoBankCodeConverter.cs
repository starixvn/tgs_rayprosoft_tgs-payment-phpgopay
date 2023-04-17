using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tgs.Payment.PhpGoPay.Client.JsonConverters;

internal class PhpGoBankCodeConverter : JsonConverter<PhpGoBankCode>
{
	public override PhpGoBankCode? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var readedBankCode = reader.GetString();

		return PhpGoBankCode.Parse(readedBankCode!);
	}

	public override void Write(Utf8JsonWriter writer, PhpGoBankCode value, JsonSerializerOptions options)
		=> writer.WriteStringValue(value.ToCode());
}
