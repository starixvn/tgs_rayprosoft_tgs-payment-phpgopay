using System.Text.Json;
using System.Text.Json.Serialization;
using Tgs.Payment.PhpGoPay.Client.JsonConverters;

namespace Tgs.Payment.PhpGoPay.Client;

public static class PhpGoPayUtility
{
	public static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
	{
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
		Converters = {
			new PhpGoPaymentTypeConverter(),
			new PhpGoBankCodeConverter(),
			new PhpGoPayWithdrawSettingsConverter()
		}
	};
}
