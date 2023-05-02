using System.Text.Json;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging.Abstractions;
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

		var sut = new DepositTimeoutCheckHandler(
			fakePaymentServiceClient,
			fakePhpGoClient,
			fakePaymentClient,
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
					"status": "3"
				}
				"""));

		// Act
		await sut.HandleAsync(request.ToByteArray());

		// Assert
		_ = fakePaymentClient.Received(1)
			.RejectDepositAsync(
				Arg.Is(request.FormId.ToGuid()),
				Arg.Any<Guid>());
	}
}
