namespace Tgs.Payment.PhpGoPay.Client;

public class PhpGoBankCode
{
	public static readonly PhpGoBankCode GCash = new("gcash");
	public static readonly PhpGoBankCode PayMaya = new("PMP");

	private static readonly Dictionary<string, PhpGoBankCode> _BankCodes = new()
	{
		["gcash"] = GCash,
		["PMP"] = PayMaya
	};

	private readonly string _code;

	private PhpGoBankCode(string code)
	{
		_code = code;
	}

	public static PhpGoBankCode? Parse(string code)
		=> _BankCodes.TryGetValue(code, out var result)
			? result
			: null;

	public string ToCode()
			=> _code;
}
