using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tgs.Payment.Clients;
using Tgs.Payment.PhpGoPay.Client;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
	.AddPaymentClient()
	.AddPhoenixToolkitsMessageQueue()
	.AddNatsMessageQueue(natsConfig => natsConfig
		.ConfigQueueOptions((opt, _) => opt.Url = "nats://localhost:4222"))
	.AddNatsGlobPatternExchange("*");

var app = builder.Build();

await app.StartAsync().ConfigureAwait(false);

var merchant = "PGTest";
var key = "32dca02f88b00f9838c88538159dab90";

var logger = app.Services.GetRequiredService<ILogger<Program>>();

var paymentClient = app.Services.GetRequiredService<IPaymentClient>();

//var depositSettings = new PhpGoPayDepositSettings(
//	merchant,
//	key,
//	PhpGoPaymentType.Direct,
//	PhpGoBankCode.PayMaya,
//	new Uri("http://localhost"),
//	Guid.NewGuid());

//var depositReturn = await paymentClient.DepositAsync(
//	"tgs.payment.phpgopay.deposit",
//	100M,
//	JsonSerializer.Serialize(depositSettings, PhpGoPayUtility.SerializerOptions)).ConfigureAwait(false);

//logger.LogInformation("DepositReturn: {depositReturn}", depositReturn);

var withdrawSettings = new PhpGoPayWithdrawSettings(
	merchant,
	key,
	Guid.NewGuid(),
	"gcash",
	"1111111",
	"1111111");

var withdrawReturn = await paymentClient.WithdrawAsync(
	"tgs.payment.phpgopay.withdraw",
	100M,
	JsonSerializer.Serialize(withdrawSettings, PhpGoPayUtility.SerializerOptions)).ConfigureAwait(false);

logger.LogInformation("{withdrawReturn}", withdrawReturn);