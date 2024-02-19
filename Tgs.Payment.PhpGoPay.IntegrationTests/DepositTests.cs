using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Tgs.Payment.Clients;
using Tgs.Payment.PhpGoPay.Client;
using Xunit.Abstractions;

namespace Tgs.Payment.PhpGoPay.IntegrationTests;

public class DepositTests(ITestOutputHelper testOutputHelper)
{
	private readonly ITestOutputHelper _testOutputHelper = testOutputHelper ?? throw new ArgumentNullException(nameof(testOutputHelper));

	[Fact]
	public async Task 儲值回呼()
	{
		var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
		var fakePaymentClient = Substitute.For<IPaymentClient>();

		await using var web = PhpGoPayTestHost.Factory
			.WithWebHostBuilder(hostBuilder => hostBuilder
				.ConfigureLogging(logging => logging.AddXUnit(_testOutputHelper))
				.ConfigureServices(services => services
					.AddSingleton(fakePaymentServiceClient)
					.AddSingleton(fakePaymentClient)));

		var httpClient = web.CreateClient();

		var formId = Guid.Parse("85d23907-8093-473a-8d9e-62bf39abf956");

		var content = new MultipartFormDataContent
		{
			{ new StringContent("PGTest"), "merchant" },
			{ new StringContent(formId.ToString()), "order_id" },
			{ new StringContent("100.0000"), "amount" },
			{ new StringContent("成功"), "message" },
			{ new StringContent("5"), "status" },
			{ new StringContent("9380ff9dbb3da59ea8008a96514f2c94"), "sign" }
		};

		var applyInfo = new PhpGoPayDepositApplyInfo(
			new PhpGoPayDepositSettings(
				"PGTest",
				"32dca02f88b00f9838c88538159dab90",
				PhpGoPaymentType.Direct,
				PhpGoBankCode.PayMaya,
				new Uri("http://localhost"),
				formId),
			 new PhpGoPayDepositResponse(
				100M,
				null,
				formId,
				new Uri("http://localhost"),
				new Uri("http://localhost")));

		_ = fakePaymentServiceClient.GetDepositFormAsync(Arg.Is(formId), Arg.Any<CancellationToken>())
			.Returns(new DepositFormInfo
			{
				Id = formId,
				BrandId = Guid.NewGuid(),
				Status = PaymentFlowState.WaitPayment,
				ApplyInfo = JsonSerializer.Serialize(applyInfo, PhpGoPayUtility.SerializerOptions)
			});

		var response = await httpClient.PostAsync("/deposit/callback", content);

		var actual = await response.Content.ReadAsStringAsync();

		Assert.Equal("SUCCESS", actual);
		_ = fakePaymentServiceClient.Received(1)
			.SubmitDepositFormStatusAsync(
				Arg.Is(formId),
				paymentReturn: Arg.Any<string>(),
				cancellationToken: Arg.Any<CancellationToken>());
		_ = fakePaymentClient.Received(1)
			.ApproveDepositAsync(
				Arg.Is(formId),
				Arg.Any<Guid>(),
				Arg.Is(applyInfo.Response.Amount),
				Arg.Any<CancellationToken>());
	}
}
