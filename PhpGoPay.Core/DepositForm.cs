using Tgs.Payment.Clients;
using Tgs.Payment.PhpGoPay.Client;

namespace Tgs.Payment.PhpGoPay;
public record DepositForm(
	Guid Id,
	string PlayerCode,
	Guid BrandId,
	Guid BrandPaymentId,
	decimal Amount,
	PaymentFlowState Status,
	PhpGoPayDepositApplyInfo? ApplyInfo,
	string? PaymentReturn = null);
