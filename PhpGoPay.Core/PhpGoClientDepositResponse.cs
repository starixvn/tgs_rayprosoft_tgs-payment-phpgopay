using System.Text.Json.Serialization;

namespace Tgs.Payment.PhpGoPay;
public record PhpGoClientDepositResponse(
	decimal Amount,
	[property: JsonConverter(typeof(IntStringJsonConverter))] string? Remark,
	[property: JsonPropertyName("order_id")] Guid FormId,
	[property: JsonPropertyName("redirect_url")] Uri RedirectUrl,
	[property: JsonPropertyName("qrcode_url")] Uri QrCodeUrl);