using System.Text.Json;
using Microsoft.Extensions.Options;
using Tgs.Payment.Clients;
using Tgs.Payment.PhpGoPay.Client;
using Tgs.Payment.Protos;
using Valhalla.MessageQueue;

namespace Tgs.Payment.PhpGoPay.MessageQueue;

internal class WithdrawProcessor(
	PhpGoClient phpGoClient,
	IPaymentClient paymentClient,
	IOptions<PhpGoOptions> optionsAccessor)
	: IMessageProcessor<WithdrawRequest, WithdrawResponse>
{
	private readonly PhpGoOptions _options = optionsAccessor.Value;

	public async ValueTask<WithdrawResponse> HandleAsync(
		string subject,
		WithdrawRequest data,
		IEnumerable<MessageHeaderValue>? headerValues,
		CancellationToken cancellationToken = default)
	{
		var settings = JsonSerializer.Deserialize<PhpGoPayWithdrawSettings>(data.ApplyInfo, PhpGoPayUtility.SerializerOptions)!;

		var order = new PhpGoClientWithdrawOrder(
			decimal.Parse(data.Amount),
			settings.FormId,
			settings.BankCode,
			settings.BankCardName,
			settings.BankCardAccount,
			new Uri($"{_options.CallbackBaseUrl}/withdraw/callback"));

		await phpGoClient.WithdrawAsync(
			settings.Merchant,
			settings.Key,
			order,
			cancellationToken).ConfigureAwait(false);

		var applyInfo = new PhpGoPayWithdrawApplyInfo(
			settings,
			new PhpGoPayWithdrawResponse());

		await paymentClient.SaveWithdrawFormThirdPartyOrderNoAsync(
			settings.FormId,
			settings.FormId.ToString(),
			cancellationToken).ConfigureAwait(false);

		return new WithdrawResponse
		{
			Result = JsonSerializer.Serialize(applyInfo, PhpGoPayUtility.SerializerOptions)
		};
	}
}
