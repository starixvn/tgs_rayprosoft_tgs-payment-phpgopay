using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tgs.Payment.PhpGoPay.Client.JsonConverters;

internal class PhpGoPaymentTypeConverter : JsonConverter<PhpGoPaymentType>
{
	public override PhpGoPaymentType? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var readPaymentType = reader.GetString();

		return PhpGoPaymentType.Parse(readPaymentType!);
	}

	public override void Write(Utf8JsonWriter writer, PhpGoPaymentType value, JsonSerializerOptions options)
		=> writer.WriteStringValue(value.ToTypeValue());
}
