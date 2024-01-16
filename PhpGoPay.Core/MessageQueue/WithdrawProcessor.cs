using System.Text.Json;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Tgs.Payment.Clients;
using Tgs.Payment.PhpGoPay.Client;
using Tgs.Payment.Protos;
using Valhalla.MessageQueue;

namespace Tgs.Payment.PhpGoPay.MessageQueue;

internal class WithdrawProcessor(
	PhpGoClient phpGoClient,
	IPaymentClient paymentClient,
	IOptions<PhpGoOptions> optionsAccessor) : IMessageProcessor<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>
{
	private readonly PhpGoClient _phpGoClient = phpGoClient ?? throw new ArgumentNullException(nameof(phpGoClient));
	private readonly IPaymentClient _paymentClient = paymentClient ?? throw new ArgumentNullException(nameof(paymentClient));
	private readonly PhpGoOptions _options = optionsAccessor.Value;

	public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(
		string subject,
		ReadOnlyMemory<byte> data,
		IEnumerable<MessageHeaderValue>? headerValues,
		CancellationToken cancellationToken = default)
	{
		var request = WithdrawRequest.Parser.ParseFrom(data.Span);
		var settings = JsonSerializer.Deserialize<PhpGoPayWithdrawSettings>(request.ApplyInfo, PhpGoPayUtility.SerializerOptions)!;

		var order = new PhpGoClientWithdrawOrder(
			decimal.Parse(request.Amount),
			settings.FormId,
			settings.BankCode,
			settings.BankCardName,
			settings.BankCardAccount,
			new Uri($"{_options.CallbackBaseUrl}/withdraw/callback"));

		await _phpGoClient.WithdrawAsync(
			settings.Merchant,
			settings.Key,
			order,
			cancellationToken).ConfigureAwait(false);

		var applyInfo = new PhpGoPayWithdrawApplyInfo(
			settings,
			new PhpGoPayWithdrawResponse());

		await _paymentClient.SaveWithdrawFormThirdPartyOrderNoAsync(
			settings.FormId,
			settings.FormId.ToString(),
			cancellationToken);

		var msg = new WithdrawResponse
		{
			Result = JsonSerializer.Serialize(applyInfo, PhpGoPayUtility.SerializerOptions)
		};

		return msg.ToByteArray();
	}
}