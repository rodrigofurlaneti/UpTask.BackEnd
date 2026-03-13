# UpTask — API v2 (Reengineering)

> **Runtime:** .NET 9 · **Architecture:** Clean Architecture + DDD · **Pattern:** CQRS via MediatR

---

## Estrutura da Solução

```
UpTask/
├── src/
│   ├── UpTask.Domain/              ← Núcleo — sem dependências externas
│   │   ├── Common/                 ← Entity base, IDomainEvent, Result pattern
│   │   ├── Entities/               ← Agregados ricos (User, Project, TaskItem, …)
│   │   ├── Enums/                  ← Enumerações de domínio
│   │   ├── Events/                 ← Domain Events (records imutáveis)
│   │   ├── Exceptions/             ← DomainException, NotFoundException, …
│   │   ├── Interfaces/             ← Contratos de repositório e UoW
│   │   └── ValueObjects/           ← Email, TaskTitle, ProjectName, HexColor
│   │
│   ├── UpTask.Application/         ← Casos de uso — orquestra o domínio
│   │   ├── Common/
│   │   │   ├── Behaviors/          ← Pipelines MediatR (Log, Validação, Perf)
│   │   │   └── Interfaces/         ← ICurrentUserService, IJwtService, IPasswordService
│   │   └── Features/
│   │       ├── Auth/Commands/      ← Register, Login, ChangePassword
│   │       ├── Projects/Commands/  ← CreateProject, UpdateProject, AddMember, …
│   │       └── Tasks/
│   │           ├── Commands/       ← CreateTask, CompleteTask, AssignTask, …
│   │           └── Queries/        ← GetTaskById, GetMyTasks, GetProjectTasks
│   │
│   ├── UpTask.Infrastructure/      ← Implementações concretas
│   │   ├── Authentication/         ← JwtService, PasswordService (BCrypt)
│   │   └── Persistence/
│   │       ├── AppDbContext.cs     ← EF Core + domain event dispatch
│   │       ├── Configurations/     ← Fluent API (Value Objects como colunas)
│   │       └── Repositories/       ← Implementações dos contratos de domínio
│   │
│   └── UpTask.API/                 ← Host HTTP — entrada e saída
│       ├── Controllers/            ← Auth, Tasks, Projects (herdam ApiController)
│       ├── Middleware/             ← GlobalExceptionMiddleware
│       ├── Services/               ← CurrentUserService (resolve JWT → ICurrentUserService)
│       └── Program.cs              ← Composition Root
│
└── tests/
    ├── UpTask.Domain.Tests/        ← xUnit · testa invariantes das entidades e VOs
    ├── UpTask.Application.Tests/   ← xUnit + Moq · testa Handlers com deps mockadas
    ├── UpTask.Integration.Tests/   ← WebApplicationFactory + InMemory DB
    └── UpTask.BDD.Tests/           ← Reqnroll (SpecFlow) · cenários em Gherkin (PT)
```

---

## Decisões Arquiteturais

### Result Pattern em vez de Exceptions para controle de fluxo
Handlers retornam `Result<T>` em vez de lançar exceções. O `ApiController` base traduz o `Error` para o status HTTP correto (404 / 401 / 409 / 422). Exceções são reservadas para falhas *inesperadas* (tratadas pelo middleware global).

### Value Objects como tipos de primeira classe
`Email`, `TaskTitle`, `ProjectName` e `HexColor` encapsulam validação e normalização. O EF Core os persiste via conversores configurados no Fluent API — sem poluição de primitivos no domínio.

### Domain Events via `IPublisher` do MediatR
O `AppDbContext.SaveChangesAsync` coleta eventos pendentes nas entidades, persiste os dados e *só então* publica os eventos. Garante consistência: os eventos refletem o estado já salvo.

### CQRS com MediatR
Cada caso de uso é um `record` (`IRequest<Result<T>>`), um `AbstractValidator<T>`, e um `Handler`. O `ValidationBehavior` do pipeline executa os validadores *antes* do handler — handlers recebem apenas dados válidos.

---

## Primeiros Passos

### Pré-requisitos
- .NET 9 SDK
- SQL Server (local ou Docker)

### Configuração

```bash
# Clone e restaure
dotnet restore

# Configure a connection string em src/UpTask.API/appsettings.json
# ou via User Secrets:
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=...;Database=UpTaskDb;..."
dotnet user-secrets set "Jwt:SecretKey" "sua-chave-de-256-bits-aqui"

# Aplique as migrations
dotnet ef database update --project src/UpTask.Infrastructure --startup-project src/UpTask.API

# Execute a API
dotnet run --project src/UpTask.API
# Swagger: https://localhost:5001/swagger
```


### Executar os Testes

```bash
# Todos os testes
dotnet test

# Apenas unitários do domínio
dotnet test tests/UpTask.Domain.Tests

# Apenas BDD
dotnet test tests/UpTask.BDD.Tests

# Com cobertura
dotnet test --collect:"XPlat Code Coverage"
```


### Criar nova Migration

```bash
dotnet ef migrations add NomeDaMigration \
  --project src/UpTask.Infrastructure \
  --startup-project src/UpTask.API \
  --output-dir Persistence/Migrations
```

---

## Principais Regras de Negócio (extraídas do sistema original)

| Entidade | Regra |
|---|---|
| **User** | Email único, normalizado para minúsculas; senha com ≥8 chars, maiúscula, dígito |
| **Project** | Dono é sempre Admin; data fim ≥ data início; progress 100% → status `Completed` automaticamente |
| **TaskItem** | Só o criador ou assignee pode completar/editar; `Complete()` obrigatório para concluir (não `ChangeStatus`) |
| **TaskItem** | Tarefas canceladas não podem ser concluídas; tarefas já concluídas não podem ser completadas novamente |
| **ProjectMember** | Assignee deve ser membro do projeto; dono não pode ser removido |
| **TimeEntry** | `EndTime > StartTime`; duração mínima de 1 minuto |
| **Comment** | Soft-delete com `DeletedAt`; comentário deletado não pode ser editado |
| **ChecklistItem** | `CompletionPercentage()` calculado pelo próprio agregado Checklist |
