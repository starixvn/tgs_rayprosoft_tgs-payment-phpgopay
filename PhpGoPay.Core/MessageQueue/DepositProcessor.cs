using System.Text.Json;
using Microsoft.Extensions.Options;
using Tgs.Payment.Clients;
using Tgs.Payment.PhpGoPay.Client;
using Tgs.Payment.Protos;
using Valhalla.MessageQueue;

namespace Tgs.Payment.PhpGoPay.MessageQueue;

internal class DepositProcessor(
	PhpGoClient phpGoClient,
	IPaymentClient paymentClient,
	IOptions<PhpGoOptions> optionsAccessor)
	: IMessageProcessor<DepositRequest, DepositResponse>
{
	private readonly PhpGoOptions _options = optionsAccessor.Value;

	public async ValueTask<DepositResponse> HandleAsync(
		string subject,
		DepositRequest data,
		IEnumerable<MessageHeaderValue>? headerValues,
		CancellationToken cancellationToken = default)
	{
		var settings = JsonSerializer.Deserialize<PhpGoPayDepositSettings>(
			data.ApplyInfo,
			PhpGoPayUtility.SerializerOptions)!;

		var order = new PhpGoClientDepositOrder(
			settings.PaymentType,
			decimal.Parse(data.Amount),
			settings.FormId,
			settings.BankCode,
			new Uri($"{_options.CallbackBaseUrl}/deposit/callback"),
			settings.ReturnUrl);

		var depositResponse = await phpGoClient.DepositAsync(
			settings.Merchant,
			settings.Key,
			order,
			cancellationToken).ConfigureAwait(false);

		var applyInfo = new PhpGoPayDepositApplyInfo(
			settings,
			new PhpGoPayDepositResponse(
				depositResponse.Amount,
				depositResponse.Remark,
				depositResponse.FormId,
				depositResponse.RedirectUrl,
				depositResponse.QrCodeUrl));

		await paymentClient.ScheduleAsync(
			$"tgs.payment.{_options.PaymentCode.ToLower()}.deposit.schedule.reject",
			settings.FormId,
			DateTime.UtcNow.AddMinutes(11D),
			cancellationToken).ConfigureAwait(false);

		return new DepositResponse
		{
			Result = JsonSerializer.Serialize(applyInfo, PhpGoPayUtility.SerializerOptions)
		};
	}
}
