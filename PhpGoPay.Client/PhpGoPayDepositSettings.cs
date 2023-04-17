using System.Text.Json.Serialization;
using Tgs.Payment.PhpGoPay.Client.JsonConverters;

namespace Tgs.Payment.PhpGoPay.Client;
public record PhpGoPayDepositSettings(
	string Merchant,
	string Key,
	[property: JsonConverter(typeof(PhpGoPaymentTypeConverter))]
	PhpGoPaymentType PaymentType,
	[property: JsonConverter(typeof(PhpGoBankCodeConverter))]
	PhpGoBankCode BankCode,
	Uri ReturnUrl,
	Guid FormId);
