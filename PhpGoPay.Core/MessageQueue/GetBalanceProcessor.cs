using System.Text.Json;
using Tgs.Payment.PhpGoPay.Client;
using Tgs.Payment.Protos;
using Valhalla.MessageQueue;

namespace Tgs.Payment.PhpGoPay.MessageQueue;

internal class GetBalanceProcessor(
	PhpGoClient phpGoClient)
	: IMessageProcessor<PaymentBalanceRequest, PaymentBalanceResponse>
{
	public async ValueTask<PaymentBalanceResponse> HandleAsync(
		string subject,
		PaymentBalanceRequest data,
		IEnumerable<MessageHeaderValue>? headerValues,
		CancellationToken cancellationToken = default)
	{
		var settings = JsonSerializer.Deserialize<BrandPaymentMerchantInfo>(
			data.BrandPaymentSettings,
			PhpGoPayUtility.SerializerOptions)!;

		var result = await phpGoClient.GetBalanceAsync(
			settings.Merchant,
			settings.Key,
			cancellationToken).ConfigureAwait(false);

		return new PaymentBalanceResponse
		{
			Balance = result.Balance
		};
	}
}
