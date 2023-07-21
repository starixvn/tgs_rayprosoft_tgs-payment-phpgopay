using System.Text.Json;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Tgs.MessageQueue;
using Tgs.Payment.Clients;
using Tgs.Payment.PhpGoPay.Client;
using Tgs.Payment.Protos;

namespace Tgs.Payment.PhpGoPay.MessageQueue;

internal class DepositProcessor : IMessageProcessor
{
	private readonly PhpGoClient _phpGoClient;
	private readonly IPaymentClient _paymentClient;
	private readonly PhpGoOptions _options;

	public DepositProcessor(
		PhpGoClient phpGoClient,
		IPaymentClient paymentClient,
		IOptions<PhpGoOptions> optionsAccessor)
	{
		_phpGoClient = phpGoClient ?? throw new ArgumentNullException(nameof(phpGoClient));
		_paymentClient = paymentClient ?? throw new ArgumentNullException(nameof(paymentClient));
		_options = optionsAccessor.Value;
	}

	public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var request = DepositRequest.Parser.ParseFrom(data.Span);

		var settings = JsonSerializer.Deserialize<PhpGoPayDepositSettings>(
			request.ApplyInfo,
			PhpGoPayUtility.SerializerOptions)!;

		var order = new PhpGoClientDepositOrder(
			settings.PaymentType,
			decimal.Parse(request.Amount),
			settings.FormId,
			settings.BankCode,
			new Uri($"{_options.CallbackBaseUrl}/deposit/callback"),
			settings.ReturnUrl);

		var depositResponse = await _phpGoClient.DepositAsync(
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

		await _paymentClient.ScheduleAsync(
			$"tgs.payment.{_options.PaymentCode.ToLower()}.deposit.schedule.reject",
			settings.FormId,
			DateTime.UtcNow.AddMinutes(11D),
			cancellationToken).ConfigureAwait(false);

		var msg = new DepositResponse
		{
			Result = JsonSerializer.Serialize(applyInfo, PhpGoPayUtility.SerializerOptions)
		};

		return msg.ToByteArray();
	}
}
