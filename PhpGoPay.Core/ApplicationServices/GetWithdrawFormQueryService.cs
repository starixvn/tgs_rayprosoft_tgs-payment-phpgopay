using System.Text.Json;
using Tgs.Payment.Clients;
using Tgs.Payment.PhpGoPay.Client;
using Tgs.ServiceUtility;

namespace Tgs.Payment.PhpGoPay.ApplicationServices;

internal class GetWithdrawFormQueryService(IPaymentServiceClient paymentServiceClient) : IGetService<GetWithdrawFormQuery, WithdrawForm?>
{
	private readonly IPaymentServiceClient _paymentServiceClient = paymentServiceClient ?? throw new ArgumentNullException(nameof(paymentServiceClient));

	public async ValueTask<WithdrawForm?> GetAsync(GetWithdrawFormQuery query, CancellationToken cancellationToken = default)
	{
		var data = await _paymentServiceClient
			.GetWithdrawFormAsync(query.FormId, cancellationToken)
			.ConfigureAwait(false);

		return data == null
			? null
			: new WithdrawForm(
				data.Id,
				data.UserId,
				data.UserType,
				data.BrandId,
				data.BrandPaymentId,
				data.Amount,
				data.Status,
				data.BankInfoId,
				string.IsNullOrEmpty(data.ApplyInfo)
					? null
					: JsonSerializer.Deserialize<PhpGoPayWithdrawApplyInfo>(data.ApplyInfo, PhpGoPayUtility.SerializerOptions),
				data.PaymentReturn);
	}
}
