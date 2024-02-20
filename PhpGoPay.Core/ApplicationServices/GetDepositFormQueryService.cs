using System.Text.Json;
using Tgs.Payment.Clients;
using Tgs.Payment.PhpGoPay.Client;
using Tgs.ServiceUtility;

namespace Tgs.Payment.PhpGoPay.ApplicationServices;

internal class GetDepositFormQueryService(IPaymentServiceClient paymentServiceClient) : IGetService<GetDepositFormQuery, DepositForm?>
{
	private readonly IPaymentServiceClient _paymentServiceClient = paymentServiceClient ?? throw new ArgumentNullException(nameof(paymentServiceClient));

	public async ValueTask<DepositForm?> GetAsync(GetDepositFormQuery query, CancellationToken cancellationToken = default)
	{
		var data = await _paymentServiceClient
			.GetDepositFormAsync(query.FormId, cancellationToken)
			.ConfigureAwait(false);

		return data == null
			? null
			: new DepositForm(
				data.Id,
				data.PlayerCode,
				data.BrandId,
				data.BrandPaymentId,
				data.Amount,
				data.Status,
				string.IsNullOrEmpty(data.ApplyInfo)
					? null
					: JsonSerializer.Deserialize<PhpGoPayDepositApplyInfo>(data.ApplyInfo, PhpGoPayUtility.SerializerOptions),
				data.PaymentReturn);
	}
}
