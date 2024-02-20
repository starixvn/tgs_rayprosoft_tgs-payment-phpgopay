using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using RichardSzalay.MockHttp;
using Tgs.Payment.Clients;
using Tgs.Payment.PhpGoPay;
using Tgs.Payment.PhpGoPay.MessageQueue;
using Tgs.Payment.Protos;

namespace PhpGoPay.Core.UnitTests;
public class GetBalanceProcessorTests
{
	[Fact]
	public async Task GetBalanceProcessor_取得商戶餘額()
	{
		// Arrange
		var mockHttp = new MockHttpMessageHandler();

		var httpClient = mockHttp.ToHttpClient();
		httpClient.BaseAddress = new Uri("https://test.galaxy.com");

		var fakePhpGoClient = new PhpGoClient(
			httpClient,
			new PhpGoHasher(NullLogger<PhpGoHasher>.Instance),
			NullLogger<PhpGoClient>.Instance);

		var sut = new GetBalanceProcessor(fakePhpGoClient);

		var brandPaymentId = Guid.NewGuid();

		var request = new PaymentBalanceRequest
		{
			BrandPaymentSettings = """
				{
					"merchant": "ATB",
					"key": "006aecc37beff9eb59a6cb03f2cf699c"
				}
				"""
		};

		_ = mockHttp.Expect(HttpMethod.Post, "https://test.galaxy.com/api/me")
			.With(req => req.Content?.Headers.ContentType?.MediaType == "application/json")
			.Respond(HttpStatusCode.OK, new StringContent($$"""
				{
				    "merchant": "ATB",
				    "merchant_display_name": "ATB",
				    "balance": "100.0000",
				    "pending_balance": 0,
				    "sign": "72615733b52a547dd5f560e1832cc6d0"
				}
				"""));

		// Act
		var actual = await sut.HandleAsync("test", request, null);

		// Assert
		Assert.Equal(100M, actual.Balance.ToDecimal());
	}
}
