namespace Tgs.Payment.PhpGoPay.Client;
public record PhpGoPayWithdrawApplyInfo(
	PhpGoPayWithdrawSettings Settings,
	PhpGoPayWithdrawResponse Response);
