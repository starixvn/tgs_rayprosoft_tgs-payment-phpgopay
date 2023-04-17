using System.Net.Mime;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Tgs.Payment.Clients;
using Tgs.Payment.PhpGoPay.Client;
using Tgs.ServiceUtility;
using Tgs.Payment.PhpGoPay;
using Tgs.Payment.PhpGoPay.ApplicationServices;

namespace Tgs.Payment.PhpGoPay.Controllers;

[Route("[controller]")]
[ApiController]
public class WithdrawController : ControllerBase
{
	private readonly ILogger<WithdrawController> _logger;

	public WithdrawController(ILogger<WithdrawController> logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	[HttpPost("callback", Name = "NotifyWithdraw")]
	[Consumes("multipart/form-data")]
	[Produces(MediaTypeNames.Text.Plain)]
	public async Task<string> WithdrawCallbackAsync(
		[FromServices] PhpGoHasher hasher,
		[FromServices] IServiceCoordinator serviceCoordinator,
		[FromServices] IPaymentServiceClient paymentServiceClient,
		[FromServices] IPaymentClient paymentClient,
		[FromForm] IFormCollection formData)
	{
		var orderId = formData["order_id"];

		if (orderId.Count == 0)
			return "SUCCESS";

		if (!Guid.TryParse(orderId, out var formId))
		{
			_logger.LogWarning("Parse formId({id}) fail.", orderId!);
			return "FAIL";
		}

		var withdrawForm = await serviceCoordinator
			.GetAsync<GetWithdrawFormQuery, WithdrawForm?>(new(formId), HttpContext.RequestAborted)
			.ConfigureAwait(false);

		if (withdrawForm == null)
		{
			_logger.LogWarning("Can't found deposit form({id}).", formId);
			return "FAIL";
		}

		var sign = formData["sign"].ToString();
		var hash = hasher.ComputeHash(
			withdrawForm.ApplyInfo!.Settings.Key,
			formData.Select(kv => new KeyValuePair<string, string?>(
				kv.Key,
				kv.Value.ToString())));

		_logger.LogInformation("Compute hash: {hash}, sign: {sign}", hash, sign);

		if (sign != hash)
		{
			_logger.LogWarning("Sign value invalid. input: {sign}, compute: {hash}", sign, hash);
			return "FAIL";
		}

		var paymentReturn = new PhpGoPaymentReturn(
			formData["merchant"].ToString(),
			formId,
			decimal.Parse(formData["amount"].ToString()),
			formData["status"].ToString(),
			formData["message"],
			formData["remark"]);

		var fsm = PaymentStateMachineFactory.CreateWithdrawStateMachine(withdrawForm.Status);

		switch (paymentReturn.Status)
		{
			case "5":
				if (fsm.CanFire(PaymentTrigger.PaymentWithdrawCompleted))
				{
					await fsm.FireAsync(PaymentTrigger.PaymentWithdrawCompleted).ConfigureAwait(false);

					await paymentServiceClient.SubmitWithdrawFormStatusAsync(
						withdrawForm.Id,
						paymentReturn: JsonSerializer.Serialize(
							paymentReturn,
							PhpGoPayUtility.SerializerOptions),
						cancellationToken: HttpContext.RequestAborted).ConfigureAwait(false);

					await paymentClient.PaidWithdrawAsync(
						withdrawForm.Id,
						withdrawForm.BrandId).ConfigureAwait(false);
				}

				break;

			case "3":
				if (fsm.CanFire(PaymentTrigger.Reject))
				{
					await fsm.FireAsync(PaymentTrigger.Reject).ConfigureAwait(false);

					await paymentServiceClient.SubmitWithdrawFormStatusAsync(
						withdrawForm.Id,
						paymentReturn: JsonSerializer.Serialize(
							paymentReturn,
							PhpGoPayUtility.SerializerOptions),
						cancellationToken: HttpContext.RequestAborted).ConfigureAwait(false);

					await paymentClient.RejectWithdrawAsync(
						withdrawForm.Id,
						withdrawForm.BrandId,
						HttpContext.RequestAborted).ConfigureAwait(false);
				}

				break;
		}

		return "SUCCESS";
	}
}
