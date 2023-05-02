using System.Text.Json.Serialization;

namespace Tgs.Payment.PhpGoPay;
public record PhpGoClientQueryResponse(
	[property: JsonPropertyName("order_id")] Guid FormId,
	decimal Amount,
	[property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] int Status,
	string Message);
