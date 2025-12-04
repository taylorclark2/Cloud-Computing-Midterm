using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System;
using Midterm.Project;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        //Application Insights (Standard)
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

    //Database Connection
    services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(
            Environment.GetEnvironmentVariable("SqlConnectionString"),
            sqlOptions => sqlOptions.EnableRetryOnFailure()
        ));
    })
    .Build();

host.Run();
