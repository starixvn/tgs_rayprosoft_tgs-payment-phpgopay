namespace Tgs.Payment.PhpGoPay.Client;

public class PhpGoPaymentType
{
	public static readonly PhpGoPaymentType QrCode = new("1");
	public static readonly PhpGoPaymentType Gateway = new("2");
	public static readonly PhpGoPaymentType Direct = new("3");

	private static readonly Dictionary<string, PhpGoPaymentType> _PaymentTypes = new()
	{
		["1"] = QrCode,
		["2"] = Gateway,
		["3"] = Direct
	};

	private readonly string _typeValue;

	private PhpGoPaymentType(string typeValue)
	{
		_typeValue = typeValue;
	}

	public static PhpGoPaymentType? Parse(string typeValue)
		=> _PaymentTypes.TryGetValue(typeValue, out var result)
			? result
			: null;

	public string ToTypeValue()
		=> _typeValue;
}
