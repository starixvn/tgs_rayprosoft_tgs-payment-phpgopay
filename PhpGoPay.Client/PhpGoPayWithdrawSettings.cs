namespace Tgs.Payment.PhpGoPay.Client;
public record PhpGoPayWithdrawSettings(
	string Merchant,
	string Key,
	Guid FormId,
	string BankCode,
	string BankCardName,
	string BankCardAccount)
{
	public static PhpGoPayWithdrawSettings CreateBankSettings(
		string merchant,
		string key,
		Guid formId,
		string bankCode,
		string name,
		string account)
		=> new(
			merchant,
			key,
			formId,
			bankCode,
			name,
			account);

	public static PhpGoPayWithdrawSettings CreateGCashSettings(
		string merchant,
		string key,
		Guid formId,
		string phone)
		=> new(
			merchant,
			key,
			formId,
			"gcash",
			phone,
			phone);
};
