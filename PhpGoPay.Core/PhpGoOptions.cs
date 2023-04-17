namespace Tgs.Payment.PhpGoPay;

public class PhpGoOptions
{
	public required Uri CallbackBaseUrl { get; set; }

	public required Uri PaymentApiUrl { get; set; }

	public required Uri PaymentServiceBaseUrl { get; set; }
}
