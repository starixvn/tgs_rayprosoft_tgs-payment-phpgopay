using System.Text.Json.Serialization;

namespace Tgs.Payment.PhpGoPay.Client;
public record PhpGoPaymentReturn(
	string Merchant,
	[property: JsonPropertyName("order_id")] Guid FormId,
	decimal Amount,
	string Status,
	string? Message,
	string? Remark);
