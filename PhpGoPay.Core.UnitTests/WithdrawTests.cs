using NSubstitute;
using PhpGoPay.Core.ApplicationServices;
using Tgs.Payment.Clients;

namespace PhpGoPay.Core.UnitTests;

public class WithdrawTests
{
	[Fact]
	public async Task 取得提款單資料()
	{
		// Arrange
		var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();

		var sut = new GetWithdrawFormQueryService(fakePaymentServiceClient);

		var query = new GetWithdrawFormQuery(Guid.NewGuid());

		// Act
		_ = await sut.GetAsync(query);

		// Assert
		_ = fakePaymentServiceClient.Received(1)
			.GetWithdrawFormAsync(Arg.Is(query.FormId));
	}
}
