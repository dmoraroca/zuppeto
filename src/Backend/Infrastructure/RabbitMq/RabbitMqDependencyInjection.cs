using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Zuppeto.Infrastructure.RabbitMq;

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

        if (string.IsNullOrWhiteSpace(snapshot.UserName) || string.IsNullOrWhiteSpace(snapshot.Password))
        {
            throw new InvalidOperationException(
                "RabbitMQ està activat però falten les credencials. Configura-les fora de Git.");
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
                ClientProvidedName = "zuppeto-api"
            };

            return factory.CreateConnection();
        });

        return services;
    }
}
