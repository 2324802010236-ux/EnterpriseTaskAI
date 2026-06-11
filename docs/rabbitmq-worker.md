# RabbitMQ Worker

EnterpriseTaskAI uses RabbitMQ for asynchronous email delivery and overdue task reminders.

## Queues

- `enterprisetask.email`: HTML email jobs consumed by `EmailJobConsumerService`.
- `enterprisetask.deadline-reminder`: overdue task notifications consumed by `DeadlineReminderJobConsumerService`.

Both queues are durable and messages are published as persistent JSON messages.

## Local RabbitMQ

Use the local RabbitMQ service with the default development account:

```text
Host: localhost
Port: 5672
UserName: guest
Password: guest
VirtualHost: /
```

On Windows, start an installed service:

```powershell
Start-Service RabbitMQ
```

Do not use the default credentials outside local development. Override secrets with user secrets or environment variables, for example:

```powershell
$env:RabbitMQ__Password = "your-local-secret"
```

## Run

Start the worker before creating accounts or testing deadline reminders:

```powershell
dotnet run --project EnterpriseTask.Worker
dotnet run --project EnterpriseTask.Api
dotnet run --project EnterpriseTask.Admin
```

Configure SMTP through `EmailSettings` using user secrets or environment variables. No real SMTP password should be committed.

## Fallback behavior

- When RabbitMQ is enabled and available, requests publish email jobs and return without waiting for SMTP.
- When RabbitMQ is disabled or publishing fails, `EmailDeliveryService` sends directly through `IEmailSender`.
- If both queue publishing and direct SMTP fail, the existing controller warning behavior remains.
- The deadline scanner marks an overdue task as `Overdue` once and writes status history before publishing reminders. If RabbitMQ is unavailable, it creates notifications directly.
