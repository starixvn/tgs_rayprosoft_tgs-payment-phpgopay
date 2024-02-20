using System.Text.Json;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
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

public class DepositTimeoutCheckHandlerTests
{
	[Fact]
	public async Task 檢查已經申請十分鐘的單子是否已經完成()
	{
		// Arrange
		var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();

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

		var sut = new DepositTimeoutCheckHandler(
			fakePaymentServiceClient,
			fakePhpGoClient,
			fakePaymentClient,
			new OptionsWrapper<PhpGoOptions>(options),
			NullLogger<DepositTimeoutCheckHandler>.Instance);

		var request = new ScheduleRequest
		{
			Subject = "test.channel",
			FormId = Guid.NewGuid(),
			ScheduleTime = Timestamp.FromDateTime(DateTime.UtcNow)
		};

		var applyInfo = new PhpGoPayDepositApplyInfo(
			new PhpGoPayDepositSettings(
				"PGTest",
				"32dca02f88b00f9838c88538159dab90",
				PhpGoPaymentType.Direct,
				PhpGoBankCode.GCash,
				new Uri("http://localhost"),
				request.FormId.ToGuid()),
			new PhpGoPayDepositResponse(
				1000M,
				null,
				request.FormId.ToGuid(),
				new Uri("http://abc.cc"),
				new Uri("http://abc.cc")));

		_ = fakePaymentServiceClient.GetDepositFormAsync(Arg.Is(request.FormId.ToGuid()))
			.Returns(new DepositFormInfo
			{
				Id = request.FormId.ToGuid(),
				Amount = 1000M,
				BrandId = Guid.NewGuid(),
				ApplyInfo = JsonSerializer.Serialize(applyInfo, PhpGoPayUtility.SerializerOptions)
			});

		_ = mockHttp.When("http://localhost/api/query")
			.Respond(new StringContent($$"""
				{
					"order_id": "{{request.FormId.ToGuid()}}",
					"amount": "1000",
					"status": 3
				}
				"""));

		// Act
		await sut.HandleAsync(
			subject: $"tgs.payment.{options.PaymentCode.ToLower()}.deposit.schedule.reject",
			request,
			null);

		// Assert
		_ = fakePaymentClient.Received(1)
			.RejectDepositAsync(
				Arg.Is(request.FormId.ToGuid()),
				Arg.Any<Guid>());
	}

	[Fact]
	public async Task 當三方支付結果不為5成功或3失敗時會繼續計時詢問()
	{
		// Arrange
		var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();

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

		var sut = new DepositTimeoutCheckHandler(
			fakePaymentServiceClient,
			fakePhpGoClient,
			fakePaymentClient,
			new OptionsWrapper<PhpGoOptions>(options),
			NullLogger<DepositTimeoutCheckHandler>.Instance);

		var request = new ScheduleRequest
		{
			Subject = "test.channel",
			FormId = Guid.NewGuid(),
			ScheduleTime = Timestamp.FromDateTime(DateTime.UtcNow)
		};

		var applyInfo = new PhpGoPayDepositApplyInfo(
			new PhpGoPayDepositSettings(
				"PGTest",
				"32dca02f88b00f9838c88538159dab90",
				PhpGoPaymentType.Direct,
				PhpGoBankCode.GCash,
				new Uri("http://localhost"),
				request.FormId.ToGuid()),
			new PhpGoPayDepositResponse(
				1000M,
				null,
				request.FormId.ToGuid(),
				new Uri("http://abc.cc"),
				new Uri("http://abc.cc")));

		_ = fakePaymentServiceClient.GetDepositFormAsync(Arg.Is(request.FormId.ToGuid()))
			.Returns(new DepositFormInfo
			{
				Id = request.FormId.ToGuid(),
				Amount = 1000M,
				BrandId = Guid.NewGuid(),
				ApplyInfo = JsonSerializer.Serialize(applyInfo, PhpGoPayUtility.SerializerOptions)
			});

		_ = mockHttp.When("http://localhost/api/query")
			.Respond(new StringContent($$"""
				{
					"order_id": "{{request.FormId.ToGuid()}}",
					"amount": "1000",
					"status": 4
				}
				"""));

		// Act
		await sut.HandleAsync(
			subject: $"tgs.payment.{options.PaymentCode}.deposit.schedule.reject",
			request,
			null);

		// Assert
		_ = fakePaymentClient.Received(1)
			.ScheduleAsync(
				Arg.Is($"tgs.payment.{options.PaymentCode}.deposit.schedule.reject"),
				Arg.Is(request.FormId.ToGuid()),
				Arg.Any<DateTime>());
	}
}