using System.Text.Json;
using Tgs.Payment.Clients;
using Tgs.Payment.PhpGoPay.Client;
using Tgs.Payment.Protos;
using Valhalla.MessageQueue;

namespace Tgs.Payment.PhpGoPay.MessageQueue;

internal class GetBalanceProcessor(
	IPaymentServiceClient paymentServiceClient,
	PhpGoClient phpGoClient)
	: IMessageProcessor<PaymentBalanceRequest, PaymentBalanceResponse>
{
	public async ValueTask<PaymentBalanceResponse> HandleAsync(
		string subject,
		PaymentBalanceRequest data,
		IEnumerable<MessageHeaderValue>? headerValues,
		CancellationToken cancellationToken = default)
	{
		var brandPayment = await paymentServiceClient.GetBrandPaymentByIdAsync(
			data.BrandPaymentId,
			cancellationToken).ConfigureAwait(false);

		var settings = JsonSerializer.Deserialize<BrandPaymentMerchantInfo>(
			brandPayment!.ExtendedData!,
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
