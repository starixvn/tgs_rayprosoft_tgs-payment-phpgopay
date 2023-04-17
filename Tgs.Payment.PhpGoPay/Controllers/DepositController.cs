using System.Net.Mime;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PhpGoPay.Core;
using PhpGoPay.Core.ApplicationServices;
using Tgs.Payment.Clients;
using Tgs.Payment.PhpGoPay.Client;
using Tgs.ServiceUtility;

namespace Tgs.Payment.PhpGoPay.Controllers;

[Route("[controller]")]
[ApiController]
public class DepositController : ControllerBase
{
	private readonly ILogger<DepositController> _logger;

	public DepositController(ILogger<DepositController> logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	[HttpPost("callback", Name = "NotifyDeposit")]
	[Consumes("multipart/form-data")]
	[Produces(MediaTypeNames.Text.Plain)]
	public async Task<string> DepositCallbackAsync(
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

		var depositForm = await serviceCoordinator
			.GetAsync<GetDepositFormQuery, DepositForm?>(new(formId), HttpContext.RequestAborted)
			.ConfigureAwait(false);

		if (depositForm == null)
		{
			_logger.LogWarning("Can't found deposit form({id}).", formId);
			return "FAIL";
		}

		var sign = formData["sign"].ToString();
		var hash = hasher.ComputeHash(
			depositForm.ApplyInfo!.Settings.Key,
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

		var fsm = PaymentStateMachineFactory.CreateDepositStateMachine(depositForm.Status);

		switch (paymentReturn.Status)
		{
			case "5":
				if (fsm.CanFire(PaymentTrigger.PaymentDepositCompleted))
				{
					await fsm.FireAsync(PaymentTrigger.PaymentDepositCompleted).ConfigureAwait(false);

					await paymentServiceClient.SubmitDepositFormStatusAsync(
						depositForm.Id,
						paymentReturn: JsonSerializer.Serialize(
							paymentReturn,
							PhpGoPayUtility.SerializerOptions),
						cancellationToken: HttpContext.RequestAborted).ConfigureAwait(false);

					await paymentClient.ApproveDepositAsync(
						depositForm.Id,
						depositForm.BrandId,
						paymentReturn.Amount,
						HttpContext.RequestAborted).ConfigureAwait(false);
				}

				break;

			case "3":
				if (fsm.CanFire(PaymentTrigger.Reject))
				{
					await fsm.FireAsync(PaymentTrigger.Reject).ConfigureAwait(false);

					await paymentServiceClient.SubmitDepositFormStatusAsync(
						depositForm.Id,
						paymentReturn: JsonSerializer.Serialize(
							paymentReturn,
							PhpGoPayUtility.SerializerOptions),
						cancellationToken: HttpContext.RequestAborted).ConfigureAwait(false);

					await paymentClient.RejectDepositAsync(
						depositForm.Id,
						depositForm.BrandId,
						HttpContext.RequestAborted).ConfigureAwait(false);
				}

				break;
		}

		return "SUCCESS";
	}
}
