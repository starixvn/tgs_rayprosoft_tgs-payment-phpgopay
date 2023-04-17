namespace Tgs.Payment.PhpGoPay.Client;
public record PhpGoPayDepositResponse(
	decimal Amount,
	string? Remark,
	Guid FormId,
	Uri RedirectUrl,
	Uri QrCodeUrl);
