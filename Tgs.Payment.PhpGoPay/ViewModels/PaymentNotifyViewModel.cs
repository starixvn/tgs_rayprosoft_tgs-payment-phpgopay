using Microsoft.AspNetCore.Mvc;

namespace Tgs.Payment.PhpGoPay.ViewModels;

public class PaymentNotifyViewModel
{
	[FromForm(Name = "merchant")]
	public required string Merchant { get; set; }

	[FromForm(Name = "order_id")]
	public Guid? FormId { get; set; }

	[FromForm(Name = "amount")]
	public decimal Amount { get; set; }

	[FromForm(Name = "status")]
	public required string Status { get; set; }

	[FromForm(Name = "message")]
	public string? Message { get; set; }

	[FromForm(Name = "remark")]
	public string? Remark { get; set; }

	[FromForm(Name = "sign")]
	public required string Sign { get; set; }
}
