# 🚀 UpTask API

> Task organizer REST API built with **.NET 9**, **DDD**, **Clean Architecture**, **CQRS** and **JWT Authentication**.

---

## 🏗️ Architecture

```
UpTask/
├── src/
│   ├── UpTask.Domain/          # Entities, Value Objects, Domain Events, Interfaces
│   ├── UpTask.Application/     # CQRS Commands/Queries, Validators, Behaviors
│   ├── UpTask.Infrastructure/  # EF Core, MySQL, JWT, Repositories
│   └── UpTask.API/             # Controllers, Middleware, Program.cs
└── tests/
    ├── UpTask.UnitTests/       # Domain + Application unit tests
    └── UpTask.IntegrationTests/ # HTTP integration tests
```

### Dependency Flow
```
API → Application → Domain
Infrastructure → Application → Domain
```

---

## ⚡ Quick Start

### Prerequisites
- .NET 9 SDK
- MySQL 8.0+

### 1. Clone and configure

```bash
git clone <repo>
cd UpTask
```

Edit `src/UpTask.API/appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=uptask_dev;User=root;Password=YOUR_PASSWORD;CharSet=utf8mb4;"
  }
}
```

### 2. Run migrations

```bash
dotnet ef migrations add InitialCreate \
  --project src/UpTask.Infrastructure \
  --startup-project src/UpTask.API

dotnet ef database update \
  --project src/UpTask.Infrastructure \
  --startup-project src/UpTask.API
```

### 3. Run the API

```bash
dotnet run --project src/UpTask.API
```

API will be available at: `https://localhost:5001`
Swagger UI: `https://localhost:5001/swagger`

---

## 🔑 Authentication

All endpoints except `/api/v1/auth/register` and `/api/v1/auth/login` require JWT.

```
Authorization: Bearer <token>
```

### Register
```http
POST /api/v1/auth/register
Content-Type: application/json

{
  "name": "Maria Silva",
  "email": "maria@example.com",
  "password": "MyPass@123",
  "confirmPassword": "MyPass@123"
}
```

### Login
```http
POST /api/v1/auth/login
Content-Type: application/json

{
  "email": "maria@example.com",
  "password": "MyPass@123"
}
```

Response:
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGci...",
    "tokenType": "Bearer",
    "expiresIn": 3600,
    "userId": "...",
    "email": "maria@example.com",
    "role": "Member"
  }
}
```

---

## 📚 API Endpoints

### Projects
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/projects` | List my projects |
| GET | `/api/v1/projects/{id}` | Get project details |
| POST | `/api/v1/projects` | Create project |
| PUT | `/api/v1/projects/{id}` | Update project |
| PATCH | `/api/v1/projects/{id}/status` | Change status |
| DELETE | `/api/v1/projects/{id}` | Delete project |
| POST | `/api/v1/projects/{id}/members` | Add member |

### Tasks
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/tasks/mine` | My assigned tasks |
| GET | `/api/v1/tasks/project/{projectId}` | Project tasks |
| GET | `/api/v1/tasks/{id}` | Task details |
| POST | `/api/v1/tasks` | Create task |
| PUT | `/api/v1/tasks/{id}` | Update task |
| PATCH | `/api/v1/tasks/{id}/status` | Change status |
| POST | `/api/v1/tasks/{id}/complete` | Complete task |
| POST | `/api/v1/tasks/{id}/assign` | Assign task |
| POST | `/api/v1/tasks/{id}/comments` | Add comment |
| DELETE | `/api/v1/tasks/{id}` | Delete task |

### Time Tracking
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/v1/time` | Log time |
| GET | `/api/v1/time/task/{taskId}` | Get task entries |
| DELETE | `/api/v1/time/{id}` | Delete entry |

### Categories & Tags
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/categories` | List categories |
| POST | `/api/v1/categories` | Create category |
| GET | `/api/v1/tags` | My tags |
| POST | `/api/v1/tags` | Create tag |
| DELETE | `/api/v1/tags/{id}` | Delete tag |

---

## 🧪 Running Tests

```bash
# Unit tests
dotnet test tests/UpTask.UnitTests

# Integration tests (requires DB)
dotnet test tests/UpTask.IntegrationTests

# All tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

---

## 🏛️ Design Patterns

| Pattern | Where | Purpose |
|---------|-------|---------|
| **DDD Aggregates** | Domain Entities | Business rules inside entities |
| **CQRS** | Application Features | Commands (write) / Queries (read) |
| **MediatR Pipeline** | Application Behaviors | Logging, Validation, Performance |
| **Repository + UoW** | Infrastructure | Data access abstraction |
| **Result Pattern** | Domain | Explicit success/failure |
| **Value Object** | Email | Immutable validated types |
| **Domain Events** | Entities | Decouple side effects |
| **Global Exception Middleware** | API | Unified error handling |

---

## 🔒 Security

- Passwords hashed with **BCrypt** (work factor 12)
- JWT signed with **HMAC SHA-256**
- Token expiry: 60 minutes (configurable)
- Role-based authorization: `Admin`, `Manager`, `Member`
- All sensitive config via environment variables in production

---

## ⚙️ Environment Variables (Production)

```bash
ConnectionStrings__DefaultConnection="Server=...;Password=SECRET;"
Jwt__Secret="YOUR_256_BIT_SECRET_KEY"
Jwt__ExpiresInMinutes="60"
```
