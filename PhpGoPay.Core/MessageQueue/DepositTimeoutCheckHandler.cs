using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tgs.Payment.Clients;
using Tgs.Payment.PhpGoPay.Client;
using Tgs.Payment.Protos;
using Valhalla.MessageQueue;

namespace Tgs.Payment.PhpGoPay.MessageQueue;

internal class DepositTimeoutCheckHandler(
	IPaymentServiceClient paymentServiceClient,
	PhpGoClient phpGoClient,
	IPaymentClient paymentClient,
	IOptions<PhpGoOptions> optionsAccessor,
	ILogger<DepositTimeoutCheckHandler> logger)
	: IMessageHandler<ScheduleRequest>
{
	private readonly PhpGoOptions _options = optionsAccessor.Value;

	public async ValueTask HandleAsync(
		string subject,
		ScheduleRequest data,
		IEnumerable<MessageHeaderValue>? headerValues,
		CancellationToken cancellationToken = default)
	{
		logger.LogInformation(
			"Deposit Schedule Handler Receive request Form Id:{request.FormId.ToGuid()}, "
				+ "Schedule Time:{request.ScheduleTime.ToDateTime():yyyy/MM/dd HH:mm:ss}",
			data.FormId.ToGuid(),
			data.ScheduleTime.ToDateTime());

		var formId = data.FormId.ToGuid();

		try
		{
			var depositForm = await paymentServiceClient
				.GetDepositFormAsync(formId, cancellationToken)
				.ConfigureAwait(false);

			if (depositForm is null)
			{
				logger.LogWarning("Deposit Form {request.FormId.ToGuid()} not found.", data.FormId.ToGuid());
				return;
			}

			var brandId = depositForm.BrandId;

			logger.LogInformation(
				"GetDepositFormAsync Form Id:{depositForm.Id}, Brand Id:{brandId}, Status:{depositForm.Status}",
				depositForm.Id,
				brandId,
				depositForm.Status);

			if (depositForm.Status >= PaymentFlowState.Reject)
				return;

			var applyInfo = JsonSerializer.Deserialize<PhpGoPayDepositApplyInfo>(depositForm.ApplyInfo!, PhpGoPayUtility.SerializerOptions)!;

			var result = await phpGoClient.QueryDepositOrWithdrawFormStatusAsync(
				applyInfo.Settings.Merchant,
				applyInfo.Settings.Key,
				formId,
				cancellationToken).ConfigureAwait(false);

			var fsm = PaymentStateMachineFactory.CreateDepositStateMachine(depositForm.Status);

			if (result.Status is 3)
			{
				if (fsm.CanFire(PaymentTrigger.Reject))
				{
					await fsm.FireAsync(PaymentTrigger.Reject).ConfigureAwait(false);

					await paymentClient
						.RejectDepositAsync(formId, brandId, cancellationToken)
						.ConfigureAwait(false);
				}
			}
			else if (result?.Status is not 5)
				await paymentClient.ScheduleAsync(
					$"tgs.payment.{_options.PaymentCode.ToLower()}.deposit.schedule.reject",
					formId,
					DateTime.UtcNow.AddMinutes(10D),
					cancellationToken).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Process deposit timeout error!");

			await paymentClient.ScheduleAsync(
				$"tgs.payment.{_options.PaymentCode.ToLower()}.deposit.schedule.reject",
				formId,
				DateTime.UtcNow.AddMinutes(10D),
				cancellationToken).ConfigureAwait(false);
		}
	}
}
