using Microsoft.Extensions.Logging.Abstractions;

namespace PhpGoPay.Core.UnitTests;

public class HashTests
{
	[Fact]
	public void HashTest()
	{
		var dict = new Dictionary<string, string?>
		{
			["merchant"] = "PGTest",
			["payment_type"] = "3",
			["amount"] = "100",
			["order_id"] = "b5c88f52-85f0-428c-a914-3124b0e4feb4",
			["bank_code"] = "gcash",
			["callback_url"] = "http://localhost/payment",
			["return_url"] = "http://localhost",
		};

		var sut = new PhpGoHasher(NullLogger<PhpGoHasher>.Instance);

		var expected = "95a36d0227f6ce9fae1598b5e71c25ed";

		var actual = sut.ComputeHash("32dca02f88b00f9838c88538159dab90", dict);

		Assert.Equal(32, actual.Length);
		Assert.Equal(expected, actual);
	}
}
