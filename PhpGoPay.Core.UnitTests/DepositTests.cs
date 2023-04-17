using NSubstitute;
using Tgs.Payment.Clients;
using Tgs.Payment.PhpGoPay.ApplicationServices;

namespace PhpGoPay.Core.UnitTests;

public class DepositTests
{
	[Fact]
	public async Task 取得存款單資料()
	{
		// Arrange
		var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();

		var sut = new GetDepositFormQueryService(fakePaymentServiceClient);

		var query = new GetDepositFormQuery(Guid.NewGuid());

		// Act
		_ = await sut.GetAsync(query);

		// Assert
		_ = fakePaymentServiceClient.Received(1)
			.GetDepositFormAsync(Arg.Is(query.FormId));
	}
}
