using System.Reflection;
using PhpGoPay.Core;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddPhpGoPay(
		this IServiceCollection services,
		Action<PhpGoOptions> configureOptions,
		Action<IHttpClientBuilder> ConfigurePhpGoClient)
	{
		ConfigurePhpGoClient?.Invoke(services.AddHttpClient<PhpGoClient>());

		return services
			.AddOptions<PhpGoOptions>()
			.Configure(configureOptions)
			.Services
			.RegisterAllServiceUtilityTypes(Assembly.GetExecutingAssembly(), ServiceLifetime.Transient)
			.AddSingleton<PhpGoHasher>();
	}
}
