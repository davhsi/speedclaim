using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SpeedClaim.Notifications.Functions.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddSingleton<SmtpEmailSender>();
    })
    .Build();

host.Run();
