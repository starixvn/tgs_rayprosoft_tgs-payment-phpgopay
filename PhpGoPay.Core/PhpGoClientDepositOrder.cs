using System.Text.Json.Serialization;
using Tgs.Payment.PhpGoPay.Client;

namespace Tgs.Payment.PhpGoPay;
internal record PhpGoClientDepositOrder(
	[property: JsonPropertyName("payment_type")] PhpGoPaymentType PaymentType,
	decimal Amount,
	[property: JsonPropertyName("order_id")] Guid FormId,
	[property: JsonPropertyName("bank_code")] PhpGoBankCode BankCode,
	[property: JsonPropertyName("callback_url")] Uri CallbackUrl,
	[property: JsonPropertyName("return_url")] Uri ReturnUrl);
