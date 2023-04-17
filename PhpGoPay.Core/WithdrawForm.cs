using Tgs.Payment.Clients;
using Tgs.Payment.PhpGoPay.Client;

namespace PhpGoPay.Core;
public record WithdrawForm(
	Guid Id,
	string? UserId,
	UserType UserType,
	Guid BrandId,
	Guid? BrandPaymentId,
	decimal Amount,
	PaymentFlowState Status,
	Guid BankInfoId,
	PhpGoPayWithdrawApplyInfo? ApplyInfo,
	string? PaymentReturn = null);
