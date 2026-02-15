1) Visão macro (C4 – Containers)

[Assistente.SPA] -> [Assistente.API]
                        | publishes events/commands
                        v
                    [RabbitMQ Broker]
                        ^
                        | consumes
                [Assistente.Worker]

2) Requisitos funcionais

- Criar tarefa para IA resolver (prompt + contexto + tipo).
- Enfileirar e despachar para worker.
- Acompanhar status: Queued → Running → Succeeded | Failed | Cancelled.
- Retentativas com backoff e limite.
- Registrar logs e custos (tokens, latência, modelo).
- Permitir cancelamento (best-effort).

3) Domínio
Entidades principais
    Task: unidade de trabalho.
    TaskAttempt: cada execução/retentativa (auditável).
    TaskArtifact: saídas (texto/arquivo/link).
    TaskEvent/Outbox: eventos para integrações (notificações, webhooks).

Estados e transições
    Created -> Queued -> (Reserved) -> Running -> Succeeded
                               \-> Failed (retry?) -> Queued
                               \-> Cancelled
                               \-> DeadLetter (excedeu retries / erro não recuperável)

4) Banco de dados

Connection
Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=007182;

Tabelas (SQL)

Tasks
    Id (guid), TenantId, DomainType, CapabilityType, TaskExecutionType, Priority, Status, PayloadJson
    IdempotencyKey (unique por tenant)
    ScheduledAt, LockedUntil, LockedBy
    MaxAttempts, AttemptCount, LastError, CreatedAt, UpdatedAt

TaskAttempts
    Id, TaskId, AttemptNo, StartedAt, EndedAt, Status
    Model, TokensIn, TokensOut, Cost, LatencyMs
    ErrorCode, ErrorDetail

TaskArtifacts
    Id, TaskId, Kind, Uri/Content, CreatedAt

OutboxMessages (se fizer integração/eventos)

5) Componentes

API (REST)
  -> TaskService (use cases)
     -> TaskRepository (EF)
     -> QueuePublisher (broker/outbox)
Worker Service (BackgroundService)
  -> QueueConsumer
     -> TaskExecutor (IA)
        -> AiClient
     -> ResultWriter (EF)
     -> EventPublisher (outbox/webhook)
Front (Angular)
  -> Task list/details
  -> realtime updates (SignalR ou SSE)


6) Contratos REST (versionado + RFC7807)
Endpoints (v1)
POST /api/v1/tasks
headers: Idempotency-Key: <string>
body: { type, priority, payload }
resp: 202 Accepted + Location: /api/v1/tasks/{id}

GET /api/v1/tasks/{id}

GET /api/v1/tasks?status=&type=&page=&pageSize=
header: X-Total-Count

POST /api/v1/tasks/{id}/cancel

GET /api/v1/tasks/{id}/attempts

GET /api/v1/tasks/{id}/artifacts

Erros: application/problem+json com traceId.

7) Documentação da API

Swagger com OpenAPI 3.0

8) Ambientes
Produção = Production
Homologação = Stagging
Desenvolveimento = Development


9) Value objects

DomainType
    DocumentProcessing
    CustomerSupport
    ComplianceCheck
    ContentCreation
    DataAnalysis
    CodeAutomation
    DecisionAutomation
    MonitoringAlert

CapabilityType
    LLM_Generation
    LLM_Classification
    LLM_Reasoning
    Vision_OCR
    Vision_ObjectDetection
    Embedding_Search
    RuleEngine
    ExternalIntegration

ExecutionType
    Sync
    Async
    Saga
    HumanInLoop
    Batch
    EventDriven

10) Broker

RabbitMQ

http://localhost:15672/#/
usuario: guest
senha: guest

services:
  rabbitmq:
    image: rabbitmq:3-management
    ports:
      - "5672:5672"
      - "15672:15672"

Contrato da mensagem
    public record TaskQueued(Guid TaskId, string Type, int Attempt, string CorrelationId);

API publicando mensagem
app.MapPost("/tasks", async (IPublishEndpoint bus) =>
{
    var taskId = Guid.NewGuid();
    await bus.Publish(new TaskQueued(taskId, "DOCUMENT_SUMMARY", 0, Guid.NewGuid().ToString("N")));
    return Results.Accepted($"/tasks/{taskId}", new { taskId });
});

Worker consumindo mensagem
public class TaskQueuedConsumer : IConsumer<TaskQueued>
{
    public async Task Consume(ConsumeContext<TaskQueued> ctx)
    {
        // Claim no DB (idempotência)
        // Executa IA/skill
        // Salva resultado/status
        await Task.CompletedTask;
    }
}

Configuração do MassTransit (API/Worker)

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<TaskQueuedConsumer>();

    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host("rabbitmq", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        cfg.ReceiveEndpoint("tasks-queued", e =>
        {
            e.ConfigureConsumer<TaskQueuedConsumer>(ctx);
            e.PrefetchCount = 16;           // “quantas mensagens na mão”
            e.ConcurrentMessageLimit = 1;   // paralelismo por instância
        });
    });
});


11) Log

Pacote: Serilog
Tipo:File
Local: pasta raiz do projeto
Adicionar log no AssistenteIaApi e no AssistenteIaApi.ServiceB.Worker

