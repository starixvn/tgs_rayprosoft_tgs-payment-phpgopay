using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using PhpGoPay.Core;
using PhpGoPay.Core.MessageQueue;

var builder = WebApplication.CreateBuilder(args);

builder.Logging
	.AddOpenTelemetry(options =>
	{
		options.IncludeFormattedMessage = true;
		options.ParseStateValues = true;
		_ = options.AddFluentdForwardExporter(fluentOptions =>
		{
			var nodeIp = builder.Configuration.GetValue<string>("NODE_IP")!;
			fluentOptions.Host = nodeIp;
			fluentOptions.Tag = builder.Configuration.GetValue<string>("OTEL_SERVICE_NAME")!;
			fluentOptions.UseMessagePack();
		});
	});

builder.Services
	.AddControllers();

builder.Services
	.AddPhpGoPay(
		options => builder.Configuration.GetSection("PhpGoPay").Bind(options),
		clientBuilder => clientBuilder
			.ConfigureHttpClient((sp, http) =>
			{
				var options = sp.GetRequiredService<IOptions<PhpGoOptions>>().Value;

				http.BaseAddress = options.PaymentApiUrl;
			})
			.AddLoggingHandler<PhpGoOptions>())
	.AddPaymentClient()
	.AddPaymentServiceClient(httpBuilder => httpBuilder
		.ConfigureHttpClient((sp, http) =>
		{
			var options = sp.GetRequiredService<IOptions<PhpGoOptions>>().Value;

			http.BaseAddress = options.PaymentServiceBaseUrl;
		}))
	.AddMessageQueue()
	.AddNatsMessageQueue(natsConfig => natsConfig
		.ConfigQueueOptions((options, _) => options.Url =
			builder.Configuration.GetConnectionString("Nats")!)
		.AddProcessor(
			ProcessorTypes.DepositProcessorType,
			"tgs.payment.phpgopay.deposit",
			"tgs.payment.phpgopay.deposit")
		.AddProcessor(
			ProcessorTypes.WithdrawProcessorType,
			"tgs.payment.phpgopay.withdraw",
			"tgs.payment.phpgopay.withdraw"))
	.AddNatsGlobPatternExchange("*");

builder.Services
	.AddProfileViewer()
	.AddHttpLogging(logging =>
	{
		logging.LoggingFields = HttpLoggingFields.All;
		logging.MediaTypeOptions.AddText("application/x-www-form-urlencoded");
		logging.MediaTypeOptions.AddText("text/html");
		logging.MediaTypeOptions.AddText("multipart/form-data");
	})
	.AddHealthChecks()
	.Services
	.AddOpenTelemetry()
	.WithTracing(traceBuilder => traceBuilder
		.SetResourceBuilder(ResourceBuilder.CreateDefault()
			.AddEnvironmentVariableDetector())
		.AddSource("Tgs.*")
		.AddAspNetCoreInstrumentation()
		.AddHttpClientInstrumentation()
		.AddProfileViewExporter()
		.AddOtlpExporter())
	.WithMetrics(meterBuiler => meterBuiler
		.AddRuntimeInstrumentation()
		.AddAspNetCoreInstrumentation()
		.AddOtlpExporter())
	.Services
	.Configure<AspNetCoreInstrumentationOptions>(options => options.Filter =
		httpContext => !httpContext.Request.Path.HasValue
			|| !httpContext.Request.Path.StartsWithSegments("/swagger")
			&& !httpContext.Request.Path.StartsWithSegments("/healthz")
			&& !httpContext.Request.Path.StartsWithSegments("/metrics")
			&& !new[] { "html", "ico", "png", "jpg" }.Any(
				ext => httpContext.Request.Path.Value.EndsWith($".{ext}")));

var app = builder.Build();

app.UseHttpLogging();
app.MapControllers();
app.MapHealthChecks("/healthz");
app.MapProfileViewer();

app.Use(async (context, next) =>
{
	await next().ConfigureAwait(false);

	var logger = context.RequestServices.GetService<ILoggerFactory>()
		?.CreateLogger("Microsoft.AspNetCore.HttpLogging.HttpLoggingMiddleware");

	if (logger?.IsEnabled(LogLevel.Information) == true)
	{
		using var stream = new StreamReader(context.Request.Body);

		_ = await stream.ReadToEndAsync().ConfigureAwait(false);
	}
});

app.Run();
