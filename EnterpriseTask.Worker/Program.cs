using EnterpriseTask.Infrastructure;
using EnterpriseTask.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<EmailJobConsumerService>();
builder.Services.AddHostedService<DeadlineReminderJobConsumerService>();
builder.Services.AddHostedService<DeadlineReminderScannerService>();

var host = builder.Build();
host.Run();
