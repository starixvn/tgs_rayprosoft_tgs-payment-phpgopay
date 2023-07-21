using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tgs.MessageQueue;
using Tgs.Payment.Clients;
using Tgs.Payment.PhpGoPay.Client;
using Tgs.Payment.Protos;

namespace Tgs.Payment.PhpGoPay.MessageQueue;

internal class DepositTimeoutCheckHandler : IMessageHandler
{
	private readonly IPaymentServiceClient _paymentServiceClient;
	private readonly PhpGoClient _phpGoClient;
	private readonly IPaymentClient _paymentClient;
	private readonly ILogger<DepositTimeoutCheckHandler> _logger;
	private readonly PhpGoOptions _options;

	public DepositTimeoutCheckHandler(
		IPaymentServiceClient paymentServiceClient,
		PhpGoClient phpGoClient,
		IPaymentClient paymentClient,
		IOptions<PhpGoOptions> optionsAccessor,
		ILogger<DepositTimeoutCheckHandler> logger)
	{
		_paymentServiceClient = paymentServiceClient ?? throw new ArgumentNullException(nameof(paymentServiceClient));
		_phpGoClient = phpGoClient ?? throw new ArgumentNullException(nameof(phpGoClient));
		_paymentClient = paymentClient ?? throw new ArgumentNullException(nameof(paymentClient));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_options = optionsAccessor.Value;
	}

	public async ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var request = ScheduleRequest.Parser.ParseFrom(data.Span);

		_logger.LogInformation(
			"Deposit Schedule Handler Receive request Form Id:{request.FormId.ToGuid()}, "
				+ "Schedule Time:{request.ScheduleTime.ToDateTime():yyyy/MM/dd HH:mm:ss}",
			request.FormId.ToGuid(),
			request.ScheduleTime.ToDateTime());

		var formId = request.FormId.ToGuid();

		try
		{
			var depositForm = await _paymentServiceClient
				.GetDepositFormAsync(formId, cancellationToken)
				.ConfigureAwait(false);

			if (depositForm is null)
			{
				_logger.LogWarning("Deposit Form {request.FormId.ToGuid()} not found.", request.FormId.ToGuid());
				return;
			}

			var brandId = depositForm.BrandId;

			_logger.LogInformation(
				"GetDepositFormAsync Form Id:{depositForm.Id}, Brand Id:{brandId}, Status:{depositForm.Status}",
				depositForm.Id,
				brandId,
				depositForm.Status);

			if (depositForm.Status >= PaymentFlowState.Reject)
				return;

			var applyInfo = JsonSerializer.Deserialize<PhpGoPayDepositApplyInfo>(depositForm.ApplyInfo!, PhpGoPayUtility.SerializerOptions)!;

			var result = await _phpGoClient.QueryDepositOrWithdrawFormStatusAsync(
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

					await _paymentClient
						.RejectDepositAsync(formId, brandId, cancellationToken)
						.ConfigureAwait(false);
				}
			}
			else if (result?.Status is not 5)
				await _paymentClient.ScheduleAsync(
					$"tgs.payment.{_options.PaymentCode.ToLower()}.deposit.schedule.reject",
					formId,
					DateTime.UtcNow.AddMinutes(10D),
					cancellationToken).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Process deposit timeout error!");

			await _paymentClient.ScheduleAsync(
				$"tgs.payment.{_options.PaymentCode.ToLower()}.deposit.schedule.reject",
				formId,
				DateTime.UtcNow.AddMinutes(10D),
				cancellationToken).ConfigureAwait(false);
		}
	}
}
