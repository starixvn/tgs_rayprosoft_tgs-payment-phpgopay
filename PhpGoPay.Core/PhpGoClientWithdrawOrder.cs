using System.Text.Json.Serialization;

namespace PhpGoPay.Core;
public record PhpGoClientWithdrawOrder(
	[property: JsonPropertyName("total_amount")] decimal Amount,
	[property: JsonPropertyName("order_id")] Guid FormId,
	[property: JsonPropertyName("bank")] string BankCode,
	[property: JsonPropertyName("bank_card_name")] string NameOrPhone,
	[property: JsonPropertyName("bank_card_account")] string AccountOrPhone,
	[property: JsonPropertyName("callback_url")] Uri CallbackUrl);
