using System.Text.Json;
using Tgs.Payment.PhpGoPay.Client;

namespace PhpGoPay.Client.UnitTests;

public class JsonSerializeTests
{
	[Fact]
	public void 序列化儲值設定()
	{
		// Arrange
		var sut = new PhpGoPayDepositSettings(
			"test123",
			"1234567890",
			PhpGoPaymentType.Direct,
			PhpGoBankCode.PayMaya,
			new Uri("http://localhost"),
			Guid.Parse("fb6a7c26-6438-4508-9440-e3f07b91dcfa"));

		var expected = """{"merchant":"test123","key":"1234567890","paymentType":"3","bankCode":"PMP","returnUrl":"http://localhost","formId":"fb6a7c26-6438-4508-9440-e3f07b91dcfa"}""";

		// Act
		var actual = JsonSerializer.Serialize(sut, PhpGoPayUtility.SerializerOptions);

		// Assert
		Assert.Equal(expected, actual);
	}

	[Fact]
	public void 反序列化儲值設定()
	{
		// Arrange
		var sut = """{"merchant":"test123","key":"1234567890","paymentType":"3","bankCode":"PMP","returnUrl":"http://localhost","formId":"fb6a7c26-6438-4508-9440-e3f07b91dcfa"}""";

		var expected = new PhpGoPayDepositSettings(
			"test123",
			"1234567890",
			PhpGoPaymentType.Direct,
			PhpGoBankCode.PayMaya,
			new Uri("http://localhost"),
			Guid.Parse("fb6a7c26-6438-4508-9440-e3f07b91dcfa"));

		// Act
		var actual = JsonSerializer.Deserialize<PhpGoPayDepositSettings>(sut, PhpGoPayUtility.SerializerOptions);

		// Assert
		Assert.Equal(expected, actual);
	}
}
