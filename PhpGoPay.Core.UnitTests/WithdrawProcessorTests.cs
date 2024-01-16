using System.Text.Json;
using Google.Protobuf;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using RichardSzalay.MockHttp;
using Tgs.Payment.Clients;
using Tgs.Payment.PhpGoPay;
using Tgs.Payment.PhpGoPay.Client;
using Tgs.Payment.PhpGoPay.MessageQueue;
using Tgs.Payment.Protos;

namespace PhpGoPay.Core.UnitTests;

public class WithdrawProcessorTests
{
	[Fact]
	public async Task 處理提款要求()
	{
		// Arrange
		var mockHttp = new MockHttpMessageHandler();

		var httpClient = mockHttp.ToHttpClient();
		httpClient.BaseAddress = new Uri("http://localhost");

		var fakePhpGoClient = new PhpGoClient(
			httpClient,
			new PhpGoHasher(NullLogger<PhpGoHasher>.Instance),
			NullLogger<PhpGoClient>.Instance);

		var fakePaymentClient = Substitute.For<IPaymentClient>();
		var options = new PhpGoOptions
		{
			CallbackBaseUrl = new Uri("https://dev-payment.rpgp.cc/phpgopay-test"),
			PaymentApiUrl = new Uri("http://localhost"),
			PaymentServiceBaseUrl = new Uri("http://localhost:5541"),
			PaymentCode = "phpgopay"
		};

		var sut = new WithdrawProcessor(
			fakePhpGoClient,
			fakePaymentClient,
			new OptionsWrapper<PhpGoOptions>(options));

		var applySettings = new PhpGoPayWithdrawSettings(
			"PGTest",
			"32dca02f88b00f9838c88538159dab90",
			Guid.NewGuid(),
			PhpGoBankCode.GCash.ToCode(),
			"123456789",
			"123456789");

		var request = new WithdrawRequest
		{
			Amount = "1000",
			ApplyInfo = JsonSerializer.Serialize(applySettings, PhpGoPayUtility.SerializerOptions)
		};

		_ = mockHttp.When("http://localhost/api/daifu")
			.Respond(new StringContent($$"""
				{
					"status": "1"
				}
				"""));

		// Act
		var actual = await sut.HandleAsync(
			"tgs.payment.phpgopay.withdraw",
			request.ToByteArray(),
			null);
		var response = WithdrawResponse.Parser.ParseFrom(actual.Span);
		var applyInfo = JsonSerializer.Deserialize<PhpGoPayWithdrawApplyInfo>(response.Result, PhpGoPayUtility.SerializerOptions);

		// Assert
		_ = fakePaymentClient.Received(1)
			.SaveWithdrawFormThirdPartyOrderNoAsync(
				Arg.Is(applySettings.FormId),
				Arg.Is(applySettings.FormId.ToString()));
		Assert.NotNull(applyInfo);
		Assert.Equal(applySettings, applyInfo.Settings);
	}
}