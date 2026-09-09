# Flow

Plataforma de **gestão do ciclo de vida da inovação corporativa**: conecta problemas operacionais a ideias, projetos formais, execução rastreada e resultados de negócio mensuráveis.

O foco do Flow é **governança, rastreabilidade e resultado**, não apenas o registro de ideias.

```text
IDEA → ANALYSIS → APPROVAL → PROJECT → EXECUTION → RESULT
```

Cada transição relevante gera registros persistidos de auditoria. Transições de projeto também geram snapshots completos do estado do projeto.

---

## Estrutura do repositório

O repositório contém a API e o aplicativo mobile do MVP.

### Backend

| Camada | Projeto | Responsabilidade |
|---|---|---|
| Domain | `Flow.Domain` | Entidades, enums, máquinas de estado e regras de negócio |
| Application | `Flow.Application` | Comandos, consultas, handlers, contratos e DTOs de aplicação |
| Infrastructure | `Flow.Infrastructure` | EF Core, Identity, JWT, persistência e migrações |
| API | `Flow.API` | Controllers, autenticação, middleware e configuração HTTP |

### Mobile

O cliente mobile está em `mobile/` e utiliza React Native com Expo.

Principais áreas:

```text
mobile/src/
  api/
  components/
  navigation/
  screens/
  store/
  types/
  utils/
  theme.ts
```

### Testes

- `tests/Flow.Application.Tests`
- `tests/Flow.API.Tests`

---

## Stack

### Backend

- .NET 8 / C#
- ASP.NET Core 8
- Entity Framework Core 8
- SQL Server / Azure SQL Database
- ASP.NET Core Identity
- JWT access + refresh token
- MediatR
- FluentValidation
- Swashbuckle / OpenAPI

### Mobile

- React Native
- Expo
- TypeScript
- React Navigation
- TanStack Query
- Zustand
- Expo SecureStore

---

## Arquitetura

O backend segue **Clean Architecture** em um **monólito modular**.

```text
Domain
  ↑
Application
  ↑
Infrastructure / API
```

As dependências apontam para dentro. O domínio não depende de frameworks nem da infraestrutura.

Módulos funcionais principais:

- Auth
- Ideas
- Projects
- Tracking
- Results
- Dashboard
- Gamification
- Guidelines

---

## Governança e rastreabilidade

A trilha de auditoria faz parte do modelo de negócio.

Regras essenciais:

- mudanças de estado passam pela lógica de domínio;
- entidades auditadas não podem ser alteradas diretamente pelo controller ou por SQL bruto;
- toda transição relevante gera `AuditLog`;
- toda transição de projeto gera `ProjectSnapshot`;
- a transição, o log e o snapshot são persistidos na mesma transação;
- rejeições, cancelamentos e bloqueios carregam contexto obrigatório no histórico.

As regras completas estão em [`ENGINEERING.md`](ENGINEERING.md).

---

## Estados principais

### Ideias

```text
Draft → UnderReview → Approved
                  └→ Rejected
```

### Projetos

```text
Planned → InProgress → Completed
   │          ├──────→ Cancelled
   │          └──────→ Blocked
   └────────────────→ Blocked

Blocked → InProgress
Blocked → Cancelled
```

`Blocked` é um estado de primeira classe e alimenta os indicadores de gargalo do dashboard.

---

## Autenticação e papéis

Papéis do MVP:

- `Operator`
- `Manager`
- `Leadership`

O registro público cria usuários como `Operator`. A atribuição de papéis elevados deve ocorrer por mecanismo administrativo controlado.

---

## Executar o backend

Pré-requisitos:

- .NET 8 SDK
- SQL Server acessível pela connection string configurada

Na raiz:

```bash
dotnet restore
dotnet run --project src/Flow.API
```

Em Development, o Swagger fica disponível em `/swagger`.

Com o perfil local padrão, os endereços normalmente utilizados são:

- `https://localhost:7296`
- `http://localhost:5153`

### Banco de dados

No startup, a aplicação aplica migrações quando utiliza banco relacional e garante a existência dos papéis de Identity necessários ao MVP.

---

## Executar os testes

```bash
dotnet test
```

A suíte cobre lógica de aplicação e integração HTTP da API.

---

## Executar o mobile

```bash
cd mobile
npm install
npm start
```

A URL da API utilizada pelo cliente deve apontar para o ambiente correto de acordo com emulador, simulador ou dispositivo físico.

---

## API

Todas as rotas estão versionadas sob `/api/v1`.

### Auth

- `POST /auth/register`
- `POST /auth/login`
- `POST /auth/refresh`
- `POST /auth/logout`

### Ideas

Inclui criação, edição em Draft, submissão, listagem, comentários, prioridade, aprovação e rejeição.

### Projects

Inclui criação independente ou a partir de uma ideia, atualização, início, conclusão, cancelamento, bloqueio, desbloqueio, timeline e snapshots.

### Results

`/projects/{projectId}/result` mantém valores estimados e reais de resultado e ROI de forma independente.

### Dashboard

`GET /dashboard/summary` expõe indicadores para Manager e Leadership, incluindo conversão, tempo médio de conclusão, ROI, distribuição de status e gargalos.

### Guidelines

CRUD de diretrizes estratégicas, com escrita restrita a Leadership.

### Users / Gamification

Consulta de pontos e ledger do operador, além de visualização controlada para Manager e Leadership.

A referência detalhada está em `docs/api/endpoints.md`.

---

## Documentação

- [`ENGINEERING.md`](ENGINEERING.md) — regras de engenharia e definição de pronto
- [`PROJECT_DECISIONS.md`](PROJECT_DECISIONS.md) — decisões atuais de produto e arquitetura
- [`docs/specs/2026-05-13-flow-mvp-design.md`](docs/specs/2026-05-13-flow-mvp-design.md) — especificação principal do MVP
- [`docs/specs/2026-05-14-flow-mobile-ui-polish-design.md`](docs/specs/2026-05-14-flow-mobile-ui-polish-design.md) — especificação visual do mobile
- `docs/architecture/` — arquitetura e decisões técnicas
- `docs/product/` — visão e fluxos do produto
- `docs/mobile/` — estrutura e telas do aplicativo
- `docs/design-system.md` — design system

---

## Estrutura resumida

```text
Flow.sln
ENGINEERING.md
PROJECT_DECISIONS.md
README.md

src/
  Flow.Domain/
  Flow.Application/
  Flow.Infrastructure/
  Flow.API/

tests/
  Flow.Application.Tests/
  Flow.API.Tests/

mobile/
  src/

docs/
  api/
  architecture/
  mobile/
  product/
  specs/
```
