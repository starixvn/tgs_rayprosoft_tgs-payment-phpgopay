using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Tgs.Payment.Clients;
using Tgs.Payment.PhpGoPay.Client;
using Xunit.Abstractions;

namespace Tgs.Payment.PhpGoPay.IntegrationTests;

public class WithdrawTests
{
	private readonly ITestOutputHelper _testOutputHelper;

	public WithdrawTests(ITestOutputHelper testOutputHelper)
	{
		_testOutputHelper = testOutputHelper ?? throw new ArgumentNullException(nameof(testOutputHelper));
	}

	[Fact]
	public async Task 代付回呼()
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

		var formId = Guid.Parse("96db344e-5593-4dcd-a8af-20207df7e019");

		var content = new MultipartFormDataContent
		{
			{ new StringContent("PGTest"), "merchant" },
			{ new StringContent(formId.ToString()), "order_id" },
			{ new StringContent("100.0000"), "amount" },
			{ new StringContent("成功"), "message" },
			{ new StringContent("5"), "status" },
			{ new StringContent("b55aba3eb2eed17cce921fc595e3d4cd"), "sign" }
		};

		var applyInfo = new PhpGoPayWithdrawApplyInfo(
			PhpGoPayWithdrawSettings.CreateGCashSettings(
				"PGTest",
				"32dca02f88b00f9838c88538159dab90",
				formId,
				"1111111"),
			 new PhpGoPayWithdrawResponse());

		_ = fakePaymentServiceClient.GetWithdrawFormAsync(Arg.Is(formId), Arg.Any<CancellationToken>())
			.Returns(new WithdrawFormInfo
			{
				Id = formId,
				BrandId = Guid.NewGuid(),
				Status = PaymentFlowState.Approved,
				ApplyInfo = JsonSerializer.Serialize(applyInfo, PhpGoPayUtility.SerializerOptions)
			});

		var response = await httpClient.PostAsync("/withdraw/callback", content);

		var actual = await response.Content.ReadAsStringAsync();

		Assert.Equal("SUCCESS", actual);
		_ = fakePaymentServiceClient.Received(1)
			.SubmitWithdrawFormStatusAsync(
				Arg.Is(formId),
				paymentReturn: Arg.Any<string>(),
				cancellationToken: Arg.Any<CancellationToken>());
		_ = fakePaymentClient.Received(1)
			.PaidWithdrawAsync(
				Arg.Is(formId),
				Arg.Any<Guid>(),
				Arg.Any<CancellationToken>());
	}
}
