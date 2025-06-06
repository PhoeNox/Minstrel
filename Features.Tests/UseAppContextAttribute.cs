namespace Features.Tests;

using Fluxor;
using Microsoft.Extensions.DependencyInjection;

public class UseAppContextAttribute : DependencyInjectionDataSourceAttribute<IServiceScope>
{
	private readonly ServiceProvider serviceProvider = CreateSharedServiceProvider();

	public override IServiceScope CreateScope(DataGeneratorMetadata dataGeneratorMetadata)
		=> serviceProvider.CreateAsyncScope();

	public override object? Create(IServiceScope scope, Type type)
		=> scope.ServiceProvider.GetService(type);

	private static ServiceProvider CreateSharedServiceProvider()
	{
		var services = new ServiceCollection();
		services.AddSingleton<AppContext>();
		services.AddSingleton<IDispatcher>(s => s.GetRequiredService<AppContext>().Services.GetRequiredService<IDispatcher>());
		return services.BuildServiceProvider();
	}
}
