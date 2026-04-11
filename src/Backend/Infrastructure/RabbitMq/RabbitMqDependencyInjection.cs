using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace YepPet.Infrastructure.RabbitMq;

public static class RabbitMqDependencyInjection
{
    public static IServiceCollection AddRabbitMq(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(RabbitMqOptions.SectionName);
        services.Configure<RabbitMqOptions>(section);

        var snapshot = section.Get<RabbitMqOptions>() ?? new RabbitMqOptions();
        if (!snapshot.Enabled)
        {
            return services;
        }

        services.AddSingleton<IConnection>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
            var factory = new ConnectionFactory
            {
                HostName = options.HostName,
                Port = options.Port,
                UserName = options.UserName,
                Password = options.Password,
                VirtualHost = string.IsNullOrEmpty(options.VirtualHost) ? "/" : options.VirtualHost,
                DispatchConsumersAsync = true,
                ClientProvidedName = "yeppet-api"
            };

            return factory.CreateConnection();
        });

        return services;
    }
}
