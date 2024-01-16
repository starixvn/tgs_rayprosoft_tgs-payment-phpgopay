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

public class DepositProcessorTests
{
	[Fact]
	public async Task 儲值要求處理()
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

		var sut = new DepositProcessor(
			fakePhpGoClient,
			fakePaymentClient,
			new OptionsWrapper<PhpGoOptions>(options));

		var applySettings = new PhpGoPayDepositSettings(
			"PGTest",
			"32dca02f88b00f9838c88538159dab90",
			PhpGoPaymentType.Direct,
			PhpGoBankCode.GCash,
			new Uri("http://localhost"),
			Guid.NewGuid());

		var request = new DepositRequest
		{
			Amount = "1000",
			ApplyInfo = JsonSerializer.Serialize(applySettings, PhpGoPayUtility.SerializerOptions)
		};

		_ = mockHttp.When("http://localhost/api/transfer")
			.Respond(new StringContent($$"""
				{
					"status": "1",
					"amount": 1000,
					"order_id": "{{applySettings.FormId}}",
					"redirect_url": "http://abc.cc",
					"qrcode_url": "http://abc.cc"
				}
				"""));

		// Act
		var actual = await sut.HandleAsync(
			"tgs.payment.phpgopay.deposit",
			request.ToByteArray(),
			null);
		var response = DepositResponse.Parser.ParseFrom(actual.Span);
		var applyInfo = JsonSerializer.Deserialize<PhpGoPayDepositApplyInfo>(response.Result, PhpGoPayUtility.SerializerOptions);

		// Assert
		_ = fakePaymentClient.Received(1)
			.ScheduleAsync(
				Arg.Is("tgs.payment.phpgopay.deposit.schedule.reject"),
				Arg.Is(applySettings.FormId),
				Arg.Any<DateTime>());
		Assert.NotNull(applyInfo);
		Assert.Equal(applySettings, applyInfo.Settings);
		Assert.Equal(
			new PhpGoPayDepositResponse(
				1000M,
				null,
				applySettings.FormId,
				new Uri("http://abc.cc"),
				new Uri("http://abc.cc")),
			applyInfo.Response);
	}
}