# agent.md — Regras padrão para Web API (.NET 9) em Clean Architecture

> Objetivo: padronizar a criação e a evolução de um projeto **ASP.NET Core 9 Web API** seguindo **Clean Architecture** (com inspiração em Ports & Adapters), priorizando manutenibilidade, testabilidade, observabilidade e segurança.

---

## 1) Padrões e decisões base

### Stack recomendada
- **.NET 9**, C# 12+
- **ASP.NET Core Web API** (Controllers ou Minimal APIs — preferir Controllers para APIs públicas e versionadas)
- **EF Core 9** + migrations
- DB: **PostgreSQL**
- Validação: **FluentValidation**
- Mapeamento: **AutoMapper** 
- Mediação/CQRS: **MediatR** (quando houver ganho real; não obrigar)
- Observabilidade: **Serilog (JSON)** + **OpenTelemetry** (traces/metrics)
- Documentação: **OpenAPI/Swagger**
- Testes: **xUnit** + **FluentAssertions**; integração com **WebApplicationFactory**
- Lint/format: `dotnet format`, analyzers, `TreatWarningsAsErrors`

### Princípios
- **S**OLID, **separação de responsabilidades**, **dependências apontam para dentro** (domínio não depende de infra).
- Domínio modelado por **capacidades do negócio** (feature/domain-first), não por camadas técnicas.
- Preferir **monólito modular** antes de microserviços.
- Acoplamento: minimizar dependências transversais; explicitar contratos.

---

## 2) Estrutura de solução (padrão)

### Nomeação
- Solution: `MyProduct`
- Projetos:
  - `MyProduct.Api`
  - `MyProduct.Application`
  - `MyProduct.Domain`
  - `MyProduct.Infrastructure`
  - `MyProduct.Contracts` *(opcional — somente se houver compartilhamento público de DTOs)*
  - `MyProduct.Tests.Unit`
  - `MyProduct.Tests.Integration`

### Estrutura de pastas sugerida (feature-first)
```
src/
  MyProduct.Api/
    Controllers/
    Middleware/
    Extensions/
    Program.cs
  MyProduct.Application/
    Common/
      Behaviors/            # pipeline (MediatR), cross-cutting
      Exceptions/
      Validation/
      Mapping/
      Abstractions/         # interfaces: IClock, ICurrentUser, IIdempotencyStore etc.
    Features/
      Orders/
        Commands/
        Queries/
        Dtos/
        Validators/
        Handlers/
  MyProduct.Domain/
    Common/
      BaseEntity.cs
      ValueObject.cs
      DomainEvent.cs
    Orders/
      Order.cs
      OrderItem.cs
      Events/
      Specifications/       # opcional
  MyProduct.Infrastructure/
    Persistence/
      AppDbContext.cs
      Configurations/       # EF Fluent configs
      Migrations/
    Repositories/           # somente se agregar valor
    Integrations/           # HTTP clients, messaging
    Identity/               # authn/authz integrations
    Observability/
tests/
  MyProduct.Tests.Unit/
  MyProduct.Tests.Integration/
```

---

## 3) Regras de camadas (Clean Architecture)

### Dependências permitidas
- `Domain` **não depende** de nenhum outro projeto.
- `Application` depende de `Domain`.
- `Infrastructure` depende de `Application` e `Domain`.
- `Api` depende de `Application` (e opcionalmente `Infrastructure` apenas para wiring/DI).

### O que vai em cada camada
**Domain**
- Entidades, Aggregates, Value Objects, Domain Events, regras invariantes.
- Zero EF Core, zero HttpClient, zero Serilog.

**Application**
- Use cases (commands/queries), orquestração, validação, regras de aplicação.
- Interfaces (ports) para dependências externas: repositórios, serviços, mensageria, clock, current user.
- DTOs/ViewModels (entrada/saída) e mapeamento.

**Infrastructure**
- EF Core DbContext, migrations, implementações de repositórios/ports, integrações externas.
- Implementações de mensageria, cache, storage, provedores de identidade, etc.

**API**
- Controllers finos: parsing de request, status codes, negociação de conteúdo, authz, encaminhar para use cases.
- Nada de regra de negócio no Controller.

---

## 4) Convenções de API REST

### Rotas
- Orientadas a recursos: `/api/v{version}/orders/{id}`
- Plural, kebab-case opcional (padrão: lowercase).
- Verbos HTTP corretos: GET/POST/PUT/PATCH/DELETE.

### Status codes
- 200/201/204 conforme operação.
- 400 validação/contrato.
- 401 não autenticado, 403 sem permissão.
- 404 recurso inexistente.
- 409 conflitos (concorrência/duplicidade).
- 422 validação semântica (quando fizer sentido).
- 500 para erros inesperados.

### Paginação/ordenação/filtro
- Padrão:
  - `?page=1&pageSize=20&sort=-createdAt`
- Resposta deve incluir:
  - Header `X-Total-Count`
  - (Opcional) `Link` para navegação.
- `pageSize` deve ter limite (ex.: 100).

### Erros padronizados (RFC 7807)
- `Content-Type: application/problem+json`
- Sempre incluir `traceId` no corpo e correlacionar com logs.

---

## 5) Validação

- Toda entrada deve ser validada na **Application**, não no Controller.
- Usar FluentValidation:
  - `NotEmpty`, limites, formatos, regras compostas.
- Regra: **falha de validação = 400** com ProblemDetails detalhado.

---

## 6) Persistência (EF Core)

- `DbContext` em `Infrastructure.Persistence`.
- Configuração de entidades via `IEntityTypeConfiguration<T>`.
- Migrations versionadas no repositório.
- Evitar `Include` indiscriminado; preferir projeções (`Select`) para queries.
- Concorrência: usar `xmin`/`ConcurrencyToken` (PostgreSQL) quando necessário.

### Repositórios
- **Não criar Repository “genérico” por padrão**.
- Criar repositórios *apenas* quando:
  - encapsula consultas complexas, ou
  - troca de persistência é plausível, ou
  - há necessidade de otimizações/abstrações claras.
- Caso contrário: usar diretamente `DbContext` em handlers.

---

## 7) CQRS/MediatR (quando usar)

- Use CQRS se houver:
  - diferenças claras entre modelos de leitura e escrita,
  - complexidade de regras e orquestrações,
  - necessidade de pipeline behaviors (logging, validation, transaction).
- Não use CQRS apenas “porque sim”.

### Transação
- Commands que alteram estado devem ser transacionais.
- Preferir transação por request (Unit of Work via behavior) quando necessário.

---

## 8) Segurança

### AuthN/AuthZ
- JWT via OAuth2/OIDC (Azure AD/B2C, Keycloak, etc.).
- Policies de autorização com `IAuthorizationHandler` quando regras forem complexas.
- Nunca confiar em dados do cliente (claims/roles devem ser validados).

### Proteções base
- CORS restritivo por ambiente.
- Rate limiting habilitado (por IP/rota), com exceções explícitas.
- Headers de segurança (HSTS em prod, etc.).
- Segredos: variables de ambiente/Key Vault — **nunca** no repositório.

### LGPD
- Minimizar PII em logs.
- Mascarar dados sensíveis.
- Auditoria: `who/when/what` (userId, timestamp, action, correlationId).

---

## 9) Observabilidade

### Logging
- Serilog estruturado (JSON).
- Todo log deve incluir `traceId`, `spanId` (via OTel) e `userId` quando disponível.
- Padrões:
  - `Information`: lifecycle de request e eventos de negócio relevantes.
  - `Warning`: condições anômalas esperadas.
  - `Error`: exceções e falhas.
  - `Fatal`: indisponibilidade crítica.

### Tracing/Metrics
- OpenTelemetry:
  - instrumentação de ASP.NET Core, HttpClient, EF Core.
  - exporter conforme ambiente (OTLP, Azure Monitor, Prometheus).

---

## 10) Documentação (Swagger/OpenAPI)

- Swagger habilitado em dev/staging (e em prod somente se autorizado).
- Versionamento de API configurado (v1, v2).
- Exemplos de request/response para endpoints críticos.
- Descrever possíveis status codes por endpoint.

---

## 11) Qualidade e testes

### Unit tests
- Foco em `Domain` e `Application` (regras e handlers).
- Evitar mocks excessivos; preferir testes com dados reais e abstrações pequenas.

### Integration tests
- `WebApplicationFactory` com banco em container (Testcontainers) quando possível.
- Testar: endpoints, auth, validação, persistência, ProblemDetails.

### Contratos
- Se houver consumidores externos: considerar Pact (contrato) e versionamento com deprecation.

---

## 12) CI/CD (mínimo)

Pipeline deve:
1. `dotnet restore`
2. `dotnet build -warnaserror`
3. `dotnet test` (unit + integration)
4. `dotnet publish`
5. Build e push de imagem Docker (tag semver + commit sha)
6. Quality gate (opcional): Sonar, SAST

---

## 13) Docker (dev/prod)

### Regras
- Dockerfile multi-stage.
- `docker-compose` para dev com:
  - API
  - DB
  - (opcional) jaeger/otel-collector
- Config por ambiente via env vars.

---

## 14) Checklist para novo endpoint (Definition of Done)

- [ ] Rota RESTful e versionada
- [ ] DTOs de request/response definidos
- [ ] Validação com FluentValidation
- [ ] Use case/handler na Application
- [ ] Domínio atualizado (entidades/invariantes) quando necessário
- [ ] Persistência/queries eficientes (projeção/paginação)
- [ ] Respostas e erros via ProblemDetails (traceId)
- [ ] Logs estruturados e spans relevantes
- [ ] Swagger atualizado
- [ ] Testes unitários + integração cobrindo cenário feliz e erros

---

## 15) Anti-padrões (proibidos)

- Regra de negócio em Controller.
- “Services” genéricos com lógica difusa.
- Repositório genérico só para “abstrair EF”.
- DTOs reaproveitados entre camadas sem intenção (vazamento de modelo).
- Expor entidades de domínio diretamente na API.
- `catch (Exception)` sem rethrow/log + ProblemDetails consistente.
- Configuração sensível no código ou no repositório.

---

## 16) Comandos úteis

```bash
# Criar solução e projetos (exemplo)
dotnet new sln -n [MyProduct]
dotnet new webapi -n [MyProduct].Api
dotnet new classlib -n [MyProduct].Domain
dotnet new classlib -n [MyProduct].Application
dotnet new classlib -n [MyProduct].Infrastructure
dotnet new xunit -n [MyProduct].Tests.Unit
dotnet new xunit -n [MyProduct].Tests.Integration

dotnet sln add src/[MyProduct].Api/[MyProduct].Api.csproj
dotnet sln add src/[MyProduct].Domain/[MyProduct].Domain.csproj
dotnet sln add src/[MyProduct].Application/[MyProduct].Application.csproj
dotnet sln add src/[MyProduct].Infrastructure/[MyProduct].Infrastructure.csproj
dotnet sln add tests/[MyProduct].Tests.Unit/[MyProduct].Tests.Unit.csproj
dotnet sln add tests/[MyProduct].Tests.Integration/[MyProduct].Tests.Integration.csproj
```

---

## 17) Observações finais

- Este arquivo é a “fonte da verdade” para padrões do projeto.
- Mudanças de arquitetura devem ser registradas em ADRs (`/docs/adr/NNNN-title.md`).
- Quando houver dúvidas: priorizar simplicidade, coesão por domínio e baixo acoplamento.
