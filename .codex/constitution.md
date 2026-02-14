Organização de pastas no projeto
ASSISTENTE-IA-API
    - Presentation
    - Application
        use-cases/
        commands/
        queries/
        dto/
        mappers/
        ports/
            in/                 # interfaces para entrada (ex: handlers)
            out/                # interfaces para saída (ex: gateway, publisher)
        services/             # serviços de aplicação (orquestração)
        validators/
    - Infrastructure
        infrastructure/
        persistence/
            orm/
            migrations/
            repositories/       # implements domain/application ports
        messaging/
            producers/
            brokers/
        http/
            server/
            middlewares/
        config/
        logging/
        monitoring/
        di/                   # dependency injection composition root
    - Interfaces/
        http/
            controllers/
            presenters/
            routes/
            request-models/
            response-models/
        cli/
        messaging/
            consumers/
        graphql/    
    - domain/
        entities/
        value-objects/
        aggregates/
        events/
        services/
        repositories/        # interfaces (ports)
        specifications/
        policies/
        exceptions/


1) Requisitos funcionais

- Criar tarefa para IA resolver (prompt + contexto + tipo).
- Enfileirar e despachar para worker.
- Acompanhar status: Queued → Running → Succeeded | Failed | Cancelled.
- Retentativas com backoff e limite.
- Registrar logs e custos (tokens, latência, modelo).
- Permitir cancelamento (best-effort).

2) Domínio
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

3) Banco de dados

Connection
Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=007182;

Tabelas (SQL)

Tasks
    Id (guid), TenantId, Type, Priority, Status, PayloadJson
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