using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using RichardSzalay.MockHttp;
using Tgs.Payment.PhpGoPay;
using Tgs.Payment.PhpGoPay.Client;
using Xunit;

namespace PhpGoPay.Core.UnitTests;

public class PhpGoClientTests
{
	[Fact]
	public async Task PhpGoClient_代收()
	{
		var mockHttp = new MockHttpMessageHandler();
		var httpClient = mockHttp.ToHttpClient();
		httpClient.BaseAddress = new Uri("https://test.galaxy.com");
		var hasher = new PhpGoHasher(NullLogger<PhpGoHasher>.Instance);

		var sut = new PhpGoClient(httpClient, hasher, NullLogger<PhpGoClient>.Instance);

		var formId = Guid.Parse("a0e5999f-93b1-49ac-a382-97061b3b5a04");
		var merchant = "PGTest";
		var key = "a8ec90c269c3bd8812739a4d710bbb85";

		var order = new PhpGoClientDepositOrder(
			PhpGoPaymentType.Direct,
			1000M,
			formId,
			PhpGoBankCode.GCash,
			new Uri("https://dev-payment.rpgp.cc/test/deposit/callback"),
			new Uri("https://op.rpgp.cc"));

		_ = mockHttp.Expect(HttpMethod.Post, "https://test.galaxy.com/api/transfer")
			.With(req => req.Content?.Headers.ContentType?.MediaType == "application/json")
			.Respond(HttpStatusCode.OK, new StringContent($$"""
				{
					"status": "1",
					"message": "success",
					"order_id": "{{formId}}",
					"amount": 1000,
					"redirect_url": "https://test.galaxy.com/pay",
					"qrcode_url": "https://test.galaxy.com/pay",
					"gcash_qr_url": "https://test.galaxy.com/pay"
				}
				"""));

		var actual = await sut.DepositAsync(merchant, key, order);

		Assert.Equal(formId, actual.FormId);
	}

	[Fact]
	public async Task PhpGoClient_代付()
	{
		var mockHttp = new MockHttpMessageHandler();
		var httpClient = mockHttp.ToHttpClient();
		httpClient.BaseAddress = new Uri("https://test.galaxy.com");
		var hasher = new PhpGoHasher(NullLogger<PhpGoHasher>.Instance);

		var sut = new PhpGoClient(httpClient, hasher, NullLogger<PhpGoClient>.Instance);

		var formId = Guid.Parse("a0e5999f-93b1-49ac-a382-97061b3b5a04");
		var merchant = "PGTest";
		var key = "a8ec90c269c3bd8812739a4d710bbb85";

		var order = new PhpGoClientWithdrawOrder(
			1000M,
			formId,
			"gcash",
			"123456789",
			"123456789",
			new Uri("https://dev-payment.rpgp.cc/test/withdraw/callback"));

		_ = mockHttp.Expect(HttpMethod.Post, "https://test.galaxy.com/api/daifu")
			.With(req => req.Content?.Headers.ContentType?.MediaType == "application/json")
			.Respond(HttpStatusCode.OK, new StringContent($$"""
				{
					"status": "1",
					"message": "success"
				}
				"""));

		await sut.WithdrawAsync(merchant, key, order);
	}

	[Fact]
	public async Task PhpGoClient_查詢訂單資訊()
	{
		// Arrange
		var mockHttp = new MockHttpMessageHandler();
		var httpClient = mockHttp.ToHttpClient();
		httpClient.BaseAddress = new Uri("https://test.galaxy.com");
		var hasher = new PhpGoHasher(NullLogger<PhpGoHasher>.Instance);

		var sut = new PhpGoClient(httpClient, hasher, NullLogger<PhpGoClient>.Instance);

		var formId = Guid.Parse("a0e5999f-93b1-49ac-a382-97061b3b5a04");
		var merchant = "PGTest";
		var key = "a8ec90c269c3bd8812739a4d710bbb85";

		_ = mockHttp.Expect(HttpMethod.Post, "https://test.galaxy.com/api/query")
			.With(req => req.Content?.Headers.ContentType?.MediaType == "application/json")
			.Respond(HttpStatusCode.OK, new StringContent($$"""
				{
					"status": "1",
					"message": "success",
					"order_id": "{{formId}}",
					"amount": 1000
				}
				"""));

		// Act
		var actual = await sut.QueryDepositOrWithdrawFormStatusAsync(merchant, key, formId);

		// Assert
		Assert.Equal(1, actual.Status);
	}

	[Fact]
	public async Task PhpGoClient_查詢帳戶餘額()
	{
		// Arrange
		var mockHttp = new MockHttpMessageHandler();
		var httpClient = mockHttp.ToHttpClient();
		httpClient.BaseAddress = new Uri("https://test.galaxy.com");
		var hasher = new PhpGoHasher(NullLogger<PhpGoHasher>.Instance);

		var sut = new PhpGoClient(httpClient, hasher, NullLogger<PhpGoClient>.Instance);

		var merchant = "ATB";
		var key = "006aecc37beff9eb59a6cb03f2cf699c";

		_ = mockHttp.Expect(HttpMethod.Post, "https://test.galaxy.com/api/me")
			.With(req => req.Content?.Headers.ContentType?.MediaType == "application/json")
			.Respond(HttpStatusCode.OK, new StringContent($$"""
				{
				    "merchant": "ATB",
				    "merchant_display_name": "ATB",
				    "balance": "3886.3700",
				    "pending_balance": 0,
				    "sign": "72615733b52a547dd5f560e1832cc6d0"
				}
				"""));

		// Act
		var actual = await sut.GetBalanceAsync(merchant, key);

		// Assert
		Assert.Equal(3886.3700M, actual.Balance);
	}
}
