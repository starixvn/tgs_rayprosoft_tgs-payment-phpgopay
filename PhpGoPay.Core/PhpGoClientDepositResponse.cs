using System.Text.Json.Serialization;

namespace PhpGoPay.Core;
public record PhpGoClientDepositResponse(
	decimal Amount,
	string? Remark,
	[property: JsonPropertyName("order_id")] Guid FormId,
	[property: JsonPropertyName("redirect_url")] Uri RedirectUrl,
	[property: JsonPropertyName("qrcode_url")] Uri QrCodeUrl);
