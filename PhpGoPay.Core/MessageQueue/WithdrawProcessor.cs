using System.Text.Json;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Tgs.MessageQueue;
using Tgs.Payment.Clients;
using Tgs.Payment.PhpGoPay.Client;
using Tgs.Payment.Protos;

namespace PhpGoPay.Core.MessageQueue;

internal class WithdrawProcessor : IMessageProcessor
{
	private readonly PhpGoClient _phpGoClient;
	private readonly IPaymentClient _paymentClient;
	private readonly PhpGoOptions _options;

	public WithdrawProcessor(
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
