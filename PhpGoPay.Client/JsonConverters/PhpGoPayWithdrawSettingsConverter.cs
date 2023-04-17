using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Tgs.Payment.PhpGoPay.Client.JsonConverters;

internal class PhpGoPayWithdrawSettingsConverter : JsonConverter<PhpGoPayWithdrawSettings>
{
	public override PhpGoPayWithdrawSettings? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var node = JsonSerializer.Deserialize<JsonNode>(ref reader, options);

		if (node is null)
			return null;

		var segment = node.Deserialize<SettingSegment>(options);

		return segment is null
			? null
			: segment.BankCode switch
			{
				"gcash" => PhpGoPayWithdrawSettings.CreateGCashSettings(
					segment.Merchant,
					segment.Key,
					segment.FormId,
					node["phone"]!.GetValue<string>()),
				_ => PhpGoPayWithdrawSettings.CreateBankSettings(
					segment.Merchant,
					segment.Key,
					segment.FormId,
					segment.BankCode,
					node["name"]!.GetValue<string>(),
					node["account"]!.GetValue<string>())
			};
	}

	public override void Write(Utf8JsonWriter writer, PhpGoPayWithdrawSettings value, JsonSerializerOptions options)
	{
		var segment = new SettingSegment(
			value.Merchant,
			value.Key,
			value.FormId,
			value.BankCode);

		var node = JsonSerializer.SerializeToNode(segment, options)!;

		switch (value.BankCode)
		{
			case "gcash":
				node["phone"] = value.BankCardAccount;
				break;
			default:
				node["name"] = value.BankCardName;
				node["account"] = value.BankCardAccount;
				break;
		}

		node.WriteTo(writer, options);
	}

	private record SettingSegment(
		string Merchant,
		string Key,
		Guid FormId,
		string BankCode);
}
