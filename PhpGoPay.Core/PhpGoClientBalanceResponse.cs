using System.Text.Json.Serialization;

namespace Tgs.Payment.PhpGoPay;
public record PhpGoClientBalanceResponse(
	string Merchant,
	[property: JsonPropertyName("merchant_display_name")] string DisplayName,
	decimal Balance,
	[property: JsonPropertyName("pending_balance")] decimal PendingBalance,
	string Sign);
