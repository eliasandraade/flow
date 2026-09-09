# Sprint 2 — Compliance Matrix

Documento de controle da Sprint 2 do Flow. Cada requisito obrigatório é rastreado desde a
auditoria inicial do código até a evidência final de verificação.

- **Branch:** `sprint-2`
- **Baseline auditado:** `ff00816a3f5b6d5f09ab3576a2ce185785e046c4` (`master`, "chore: establish clean project baseline")
- **Baseline de testes:** 121 testes verdes (82 `Flow.Application.Tests` + 39 `Flow.API.Tests`)
- **Baseline de build:** `dotnet build` com êxito, 16 avisos `NU1603` (pin inexistente de `*.IdentityModel.Tokens 8.3.4`)
- **Data limite da entrega:** 21/09/2026 23:00

## Estados

| Estado | Significado |
|---|---|
| `NOT_STARTED` | Não existe implementação. |
| `PARTIAL` | Existe implementação parcial ou que não cobre o requisito integralmente. |
| `IMPLEMENTED` | Implementado e compilando, ainda sem evidência de teste registrada. |
| `VERIFIED` | Implementado, testado e com evidência registrada nesta matriz. |

> A Sprint **não** é encerrada enquanto houver requisito obrigatório diferente de `VERIFIED`.

---

## 1. Sumário por área

| Área | Requisitos | Estado inicial predominante |
|---|---|---|
| AUTH | 8 | PARTIAL |
| STRATEGY | 9 | PARTIAL |
| IDEAS | 10 | PARTIAL |
| PROJECTS | 7 | PARTIAL |
| RESULTS | 6 | PARTIAL |
| DASHBOARD | 6 | PARTIAL |
| DATABASE | 7 | NOT_STARTED |
| MOBILE_INTEGRATION | 10 | PARTIAL |
| EXTERNAL_SERVICE | 5 | NOT_STARTED |
| OBSERVABILITY | 7 | NOT_STARTED |
| SECURITY | 9 | PARTIAL |
| AI_PLUS | 10 | NOT_STARTED |
| APK | 4 | NOT_STARTED |
| DOCUMENTATION | 10 | PARTIAL |
| DELIVERABLES | 5 | NOT_STARTED |
| **Total** | **113** | — |

---

## 2. Achados relevantes da auditoria inicial

Estes achados foram obtidos lendo o código real, não a documentação.

| # | Achado | Impacto |
|---|---|---|
| A1 | `Flow.Application` referencia `Microsoft.EntityFrameworkCore` e `IApplicationDbContext` expõe `DbSet<T>`. | Vazamento de infraestrutura para a camada de aplicação. Bloqueia a migração limpa para MongoDB. |
| A2 | `Flow.Domain` referencia `Microsoft.Extensions.Identity.Stores` porque `User : IdentityUser<Guid>`. | Contradiz "Domain sem dependência de framework" declarado em `ENGINEERING.md`. |
| A3 | Não existe endpoint para definir prioridade de ideia, embora `Idea.SetPriority` exista no domínio. | Capacidade documentada no README sem superfície de API. |
| A4 | Não existe endpoint para excluir ideia em `Draft`. | Requisito explícito do Operator na Sprint 2. |
| A5 | `FluentValidation` está referenciado em `Flow.Application.csproj` mas nenhum validador existe e nenhum behavior do MediatR está registrado. | Dependência morta; a validação hoje depende só do domínio e de `[ApiController]`. |
| A6 | `ICacheService`, `IAuthProvider` e `INotificationService` não possuem implementação registrada. | Interfaces sem uso real. `INotificationService` passa a ter uso real na Sprint 2. |
| A7 | `SaveChangesWithAuditAsync` chama `SaveChangesAsync` sem transação explícita. A atomicidade vem do `SaveChanges` único do EF. | Ao migrar para Mongo, a atomicidade precisa de sessão/transação explícita ou a garantia é perdida. |
| A8 | `mobile/src/api/client.ts` tem `API_BASE` fixo em `http://10.0.2.2:5153/api/v1` e limpa a sessão em **qualquer** 401. | Sem configuração por ambiente e sem refresh transparente. |
| A9 | A interface do mobile está em inglês. | A Sprint 2 exige pt-BR. |
| A10 | `Program.cs` não configura CORS, rate limiting, health checks, Serilog nem OpenTelemetry. | Requisitos de segurança e observabilidade ausentes. |
| A11 | Os testes de integração usam `Microsoft.EntityFrameworkCore.InMemory`. | Precisa migrar para Mongo real descartável (Testcontainers). |
| A12 | `ProjectSnapshot` e `AuditLog` são append-only por convenção, não por restrição técnica. | Precisa ser reforçado na camada de persistência Mongo. |
| A13 | Docker Desktop instalado porém com o daemon parado no ambiente de desenvolvimento. | Bloqueia replica set, Testcontainers e build de imagem até ser iniciado. |
| A14 | `victory-native@42.x` exige `@shopify/react-native-skia >=2.6.0 <3.0.0`; o Expo SDK 54 fixa `2.2.12`. | Conflito real de peer dependency. Exige spike antes de adotar. A linha `41.x` (`skia >=1.2.3`) é a candidata compatível. |
| A15 | 16 avisos `NU1603`: pin de `Microsoft.IdentityModel.Tokens` e `System.IdentityModel.Tokens.Jwt` em `8.3.4`, versão inexistente. | Ruído de build; corrigir o pin para uma versão publicada. |

---

## 3. Decisões de pesquisa confirmadas

Versões verificadas contra os registries oficiais em 09/09/2026, não por memória.

| Item | Decisão | Verificação |
|---|---|---|
| Driver Mongo | `MongoDB.Driver` 3.11.1 | NuGet flat-container. Driver oficial. Sem provider EF sobre Mongo. |
| SDK Gemini | `Google.GenAI` 1.21.0 | NuGet: publicado pelo Google, prefixo reservado, repositório `googleapis/dotnet-genai`, suporta `net8.0`. |
| Modelo Gemini | `gemini-3.8-flash` | Confirmado GA na documentação oficial do Gemini API. Contexto de 1M tokens, thinking configurável. |
| Testes Mongo | `Testcontainers.MongoDb` 4.15.0 | NuGet. Usado com replica set de nó único para exercitar transações reais. |
| Observabilidade | `Serilog.AspNetCore` 10.0.0, `OpenTelemetry.*` 1.18.0 | NuGet. |
| Instrumentação Mongo | `MongoDB.Driver.Core.Extensions.DiagnosticSources` 3.0.0 | NuGet. Compatível com a linha 3.x do driver. |
| Health check Mongo | `AspNetCore.HealthChecks.MongoDb` 9.0.0 | NuGet. |
| Push | `react-native-onesignal` 5.5.10 + `onesignal-expo-plugin` 2.7.1 | npm. Exige development build; não funciona em Expo Go. |
| Charts | Spike obrigatório antes de fixar (ver A14) | npm: peer deps de `victory-native` 41.x vs 42.x e `bundledNativeModules.json` do SDK 54. |

---

## 4. Matriz detalhada

Cada requisito registra: **Fonte/requisito · Estado atual · Evidência no código · Gap · Implementação proposta · Teste necessário · Evidência final · Status**.

O campo *Evidência final* é preenchido no fechamento de cada fase.

---

### 4.1 AUTH

#### AUTH-01 — Registro público cria Operator
- **Fonte/requisito:** Challenge (autenticação); brief §7.
- **Estado atual:** IMPLEMENTED no baseline.
- **Evidência no código:** `src/Flow.Application/Auth/Commands/Register/RegisterCommandHandler.cs`; `AuthController.Register`.
- **Gap:** Depende de `UserManager` sobre EF; precisa continuar funcionando sobre stores Mongo.
- **Implementação proposta:** Preservar o handler; trocar apenas o storage do Identity.
- **Teste necessário:** Unit do handler + integração `POST /auth/register` retornando papel `Operator`.
- **Evidência final:** _pendente_
- **Status:** `PARTIAL`

#### AUTH-02 — Login com JWT
- **Fonte/requisito:** Challenge (JWT).
- **Estado atual:** IMPLEMENTED no baseline.
- **Evidência no código:** `LoginCommandHandler.cs`; `Flow.Infrastructure/Auth/JwtTokenService.cs`.
- **Gap:** Nenhum funcional; validar após a migração.
- **Implementação proposta:** Manter; adicionar validação de força do segredo.
- **Teste necessário:** Login válido, credencial inválida, claims de papel.
- **Evidência final:** _pendente_
- **Status:** `PARTIAL`

#### AUTH-03 — Refresh token
- **Fonte/requisito:** Challenge; brief §18.
- **Estado atual:** IMPLEMENTED no baseline.
- **Evidência no código:** `RefreshTokenCommandHandler.cs`; `Flow.Domain/Entities/RefreshToken.cs`.
- **Gap:** Token persistido em claro; sem rotação explícita.
- **Implementação proposta:** Persistir hash do token, emitir novo par a cada refresh e revogar o anterior.
- **Teste necessário:** Refresh válido, expirado, revogado e reuso após rotação.
- **Evidência final:** _pendente_
- **Status:** `PARTIAL`

#### AUTH-04 — Logout com revogação
- **Estado atual:** IMPLEMENTED. **Evidência:** `LogoutCommandHandler.cs`.
- **Gap:** Revalidar sobre Mongo. **Proposta:** manter. **Teste:** logout revoga e o refresh seguinte falha.
- **Evidência final:** _pendente_ · **Status:** `PARTIAL`

#### AUTH-05 — Três perfis e autorização por nível
- **Estado atual:** IMPLEMENTED. **Evidência:** `UserRole.cs`; `[Authorize(Roles = ...)]` nos controllers.
- **Gap:** A cobertura de teste de 403 é parcial. **Proposta:** manter e ampliar os testes.
- **Teste:** matriz de acesso por papel em todos os endpoints sensíveis.
- **Evidência final:** _pendente_ · **Status:** `PARTIAL`

#### AUTH-06 — Rotação e revogação seguras de refresh token
- **Estado atual:** `PARTIAL`. **Gap:** sem rotação nem hash.
- **Proposta:** hash SHA-256 no armazenamento, índice único, rotação obrigatória e revogação em cascata na reutilização.
- **Teste:** o reuso de um token rotacionado deve falhar.
- **Evidência final:** _pendente_ · **Status:** `PARTIAL`

#### AUTH-07 — Seeds de demonstração idempotentes sob `SEED_DEMO_DATA`
- **Estado atual:** `NOT_STARTED`. O baseline só semeia os papéis do Identity (`Program.cs`).
- **Proposta:** seeder idempotente com os três usuários de demonstração e narrativa de dados.
- **Teste:** executar duas vezes não duplica dados.
- **Evidência final:** _pendente_ · **Status:** `NOT_STARTED`

#### AUTH-08 — ASP.NET Core Identity sobre MongoDB
- **Estado atual:** `NOT_STARTED`. **Evidência:** `AddEntityFrameworkStores<ApplicationDbContext>()` em `Flow.Infrastructure/DependencyInjection.cs`.
- **Proposta:** implementar `IUserStore`, `IUserPasswordStore`, `IUserEmailStore`, `IUserRoleStore`, `IUserSecurityStampStore` e `IRoleStore` sobre `MongoDB.Driver`, sem pacote comunitário.
- **Teste:** integração cobrindo criação, busca por e-mail normalizado, atribuição de papel e verificação de senha.
- **Evidência final:** _pendente_ · **Status:** `NOT_STARTED`

---

### 4.2 STRATEGY

#### STR-01 — CRUD de diretriz restrito a Leadership
- **Estado atual:** IMPLEMENTED. **Evidência:** `GuidelinesController.cs` com `[Authorize(Roles = "Leadership")]` em `POST`, `PUT` e `DELETE`.
- **Gap:** nenhum. **Teste:** CRUD por Leadership e 403 para os demais.
- **Evidência final:** _pendente_ · **Status:** `PARTIAL`

#### STR-02 — Leitura por Operator e Manager
- **Estado atual:** IMPLEMENTED. **Evidência:** `[Authorize]` no controller, sem restrição de papel no `GET`.
- **Evidência final:** _pendente_ · **Status:** `PARTIAL`

#### STR-03 — `Category`
- **Estado atual:** `NOT_STARTED`. **Evidência:** `StrategicGuideline.cs` só tem `Title`, `Description` e `CreatedBy`.
- **Proposta:** adicionar `Category` com validação de domínio. **Teste:** filtro por categoria.
- **Evidência final:** _pendente_ · **Status:** `NOT_STARTED`

#### STR-04 — `Campaign`
- **Estado atual:** `NOT_STARTED`. **Proposta:** campo opcional e agrupamento no dashboard.
- **Teste:** performance por campanha no dashboard. · **Status:** `NOT_STARTED`

#### STR-05 — `ValidFrom` e `ValidUntil` com vigência derivada
- **Estado atual:** `NOT_STARTED`. **Proposta:** vigência calculada a partir do período, sem `IsActive` mutável.
- **Teste:** limites de vigência (antes, durante, depois e `ValidUntil` nulo). · **Status:** `NOT_STARTED`

#### STR-06 — Histórico consultável
- **Estado atual:** `NOT_STARTED`. **Proposta:** coleção `strategic_guideline_history` append-only alimentada em cada alteração.
- **Teste:** um update gera entrada de histórico. · **Status:** `NOT_STARTED`

#### STR-07 — Endpoint de estratégia vigente
- **Estado atual:** `NOT_STARTED`. **Proposta:** `GET /api/v1/guidelines/current`.
- **Teste:** retorna apenas as vigentes na data de referência. · **Status:** `NOT_STARTED`

#### STR-08 — Filtros por categoria, vigência e campanha
- **Estado atual:** `NOT_STARTED`. **Proposta:** query string em `GET /guidelines` e índices Mongo.
- **Teste:** cada filtro isolado e combinado. · **Status:** `NOT_STARTED`

#### STR-09 — Validação de existência ao vincular ideia ou projeto
- **Estado atual:** `PARTIAL`. **Evidência:** `Idea.LinkedGuidelineId` existe, mas `CreateIdeaCommandHandler` não valida a diretriz.
- **Proposta:** validar existência e registrar a diretriz aplicável na conversão para projeto.
- **Teste:** vincular diretriz inexistente retorna 404 ou 422. · **Status:** `PARTIAL`

---

### 4.3 IDEAS

#### IDEA-01 — Criar, editar e excluir Draft
- **Estado atual:** `PARTIAL`. **Evidência:** `CreateIdea` e `UpdateIdea` existem; **não existe** comando nem rota de exclusão (achado A4).
- **Proposta:** `DeleteIdeaCommand` restrito a Draft e ao autor.
- **Teste:** excluir Draft próprio, 403 para terceiros e 409 fora de Draft. · **Status:** `PARTIAL`

#### IDEA-02 — Enviar para análise
- **Estado atual:** IMPLEMENTED. **Evidência:** `SubmitIdeaCommandHandler.cs`. · **Status:** `PARTIAL`

#### IDEA-03 — Fila do gestor com filtros
- **Estado atual:** `PARTIAL`. **Evidência:** `GetIdeasQueryHandler` lista sem filtros ricos.
- **Proposta:** filtros por status, prioridade, score e diretriz, com paginação. · **Status:** `PARTIAL`

#### IDEA-04 — Comentários
- **Estado atual:** IMPLEMENTED. **Evidência:** `AddIdeaComment*` e `GetIdeaComments*`. · **Status:** `PARTIAL`

#### IDEA-05 — Priorização pelo gestor
- **Estado atual:** `PARTIAL` (achado A3). **Evidência:** `Idea.SetPriority` sem endpoint.
- **Proposta:** `PATCH /ideas/{id}/priority`. **Teste:** priorizar e auditar. · **Status:** `PARTIAL`

#### IDEA-06 — `Score` 0..100 distinto de `Priority`
- **Estado atual:** `NOT_STARTED`. **Proposta:** campo `Score` com validação de faixa e endpoint dedicado.
- **Teste:** limites 0, 100, -1 e 101. · **Status:** `NOT_STARTED`

#### IDEA-07 — Aprovar e rejeitar
- **Estado atual:** IMPLEMENTED. **Evidência:** `ApproveIdeaCommandHandler.cs` e `RejectIdeaCommandHandler.cs`. · **Status:** `PARTIAL`

#### IDEA-08 — Comparação de ideias
- **Estado atual:** `NOT_STARTED`. **Proposta:** query de comparação lado a lado com os componentes do FlowScore. · **Status:** `NOT_STARTED`

#### IDEA-09 — Auditoria de decisões gerenciais
- **Estado atual:** IMPLEMENTED para aprovar e rejeitar. **Gap:** priorização e score também precisam auditar. · **Status:** `PARTIAL`

#### IDEA-10 — FlowScore explicável
- **Estado atual:** `NOT_STARTED`. **Proposta:** motor determinístico 0..100 com componentes visíveis e fórmula documentada em `docs/sprint-2/flowscore.md`; independente do Gemini; a entrada manual permanece soberana.
- **Teste:** unit da fórmula, monotonicidade por dimensão, limites e ordenação A vs B. · **Status:** `NOT_STARTED`

---

### 4.4 PROJECTS

#### PRJ-01 — State machine preservada
- **Estado atual:** IMPLEMENTED. **Evidência:** `Flow.Domain/Entities/Project.cs`.
- **Gap:** não regredir durante a migração. **Teste:** suíte de transições válidas e inválidas. · **Status:** `PARTIAL`

#### PRJ-02 — `StrategicGuidelineId`
- **Estado atual:** `NOT_STARTED`. **Proposta:** vínculo explícito registrado na criação e na conversão. · **Status:** `NOT_STARTED`

#### PRJ-03 — `Stage` distinto de `Status`
- **Estado atual:** `NOT_STARTED`. **Proposta:** enum `ProjectStage` (Discovery → Planning → Execution → Validation → Rollout) documentado. · **Status:** `NOT_STARTED`

#### PRJ-04 — `ProgressPercentage` 0..100 com operação dedicada
- **Estado atual:** `NOT_STARTED`. **Proposta:** comando próprio; `Completed` implica 100.
- **Teste:** limites, invariante de conclusão e auditoria. · **Status:** `NOT_STARTED`

#### PRJ-05 — Criação manual e a partir de ideia aprovada
- **Estado atual:** IMPLEMENTED. **Evidência:** `CreateProjectCommandHandler.cs` e `ConvertIdeaToProjectCommandHandler.cs`. · **Status:** `PARTIAL`

#### PRJ-06 — Rastreabilidade `strategy → idea → project → result`
- **Estado atual:** `PARTIAL`. **Gap:** falta o elo de estratégia. · **Status:** `PARTIAL`

#### PRJ-07 — Snapshots e timeline
- **Estado atual:** IMPLEMENTED. **Evidência:** `GetProjectSnapshots*` e `GetProjectTimeline*`.
- **Gap:** a imutabilidade precisa ser garantida na camada Mongo. · **Status:** `PARTIAL`

---

### 4.5 RESULTS

#### RES-01 — Estimated e Actual independentes
- **Estado atual:** IMPLEMENTED. **Evidência:** `Result.SetEstimated` e `Result.SetActual`. · **Status:** `PARTIAL`

#### RES-02 — ROI e `PaybackPeriodMonths`
- **Estado atual:** IMPLEMENTED. **Evidência:** `Result.ComputeRoi`; divisão por zero retorna `null`. · **Status:** `PARTIAL`

#### RES-03 — `ProductivityGainPercent` · RES-04 — `TimeSavedHours` · RES-05 — `QualityGainPercent`
- **Estado atual:** `NOT_STARTED` nos três. **Proposta:** adicionar ao agregado com validação de faixa e refletir no dashboard.
- **Teste:** limites e persistência independente de Estimated e Actual. · **Status:** `NOT_STARTED`

#### RES-06 — Validações de domínio
- **Estado atual:** `PARTIAL`. **Gap:** valores negativos não são rejeitados hoje.
- **Proposta:** invariantes explícitas. · **Status:** `PARTIAL`

---

### 4.6 DASHBOARD

#### DASH-01 — `GET /dashboard/summary` robusto
- **Estado atual:** `PARTIAL`. **Evidência:** `GetDashboardSummaryQueryHandler.cs` cobre 12 métricas.
- **Gap:** faltam stage, atrasados, em risco, distribuição por estratégia, campanha, rankings, tendências, produtividade e horas economizadas.
- **Teste:** base vazia e base realista. · **Status:** `PARTIAL`

#### DASH-02 — Datasets prontos para visualização
- **Estado atual:** `NOT_STARTED`. **Proposta:** séries e distribuições prontas para gráfico, sem reagregação no mobile. · **Status:** `NOT_STARTED`

#### DASH-03 — Aggregation pipelines sem N+1
- **Estado atual:** `NOT_STARTED`. **Evidência do problema:** o baseline faz cerca de 8 roundtrips sequenciais.
- **Proposta:** `$facet` para consolidar em poucas idas ao banco. · **Status:** `NOT_STARTED`

#### DASH-04 — `GET /dashboard/projects/{id}` · DASH-05 — `GET /dashboard/strategies/{id}`
- **Estado atual:** `NOT_STARTED` em ambos. · **Status:** `NOT_STARTED`

#### DASH-06 — Rankings e tendências temporais
- **Estado atual:** `NOT_STARTED`. · **Status:** `NOT_STARTED`

---

### 4.7 DATABASE

#### DB-01 — Remoção operacional de EF Core e SQL Server
- **Estado atual:** `NOT_STARTED`. **Evidência:** `ApplicationDbContext`, 3 migrations, `UseSqlServer` e `Microsoft.EntityFrameworkCore.SqlServer`.
- **Proposta:** remover `DbContext`, configurations, migrations e pacotes EF de `src/`.
- **Teste:** ausência de referência a EF em `src/` verificada por busca. · **Status:** `NOT_STARTED`

#### DB-02 — `MongoDB.Driver` oficial
- **Estado atual:** `NOT_STARTED`. **Proposta:** driver 3.11.1 com `MongoClient` singleton. · **Status:** `NOT_STARTED`

#### DB-03 — Modelo documental derivado dos access patterns
- **Estado atual:** `NOT_STARTED`. **Proposta:** documentar em `docs/sprint-2/data-model.md` antes de codificar. · **Status:** `NOT_STARTED`

#### DB-04 — Índices explícitos
- **Estado atual:** `NOT_STARTED`. **Proposta:** criação idempotente no startup para os padrões listados no brief §5. · **Status:** `NOT_STARTED`

#### DB-05 — Transações multi-documento em replica set
- **Estado atual:** `NOT_STARTED`. **Proposta:** replica set de nó único em desenvolvimento e teste; compose com `rs.initiate`. · **Status:** `NOT_STARTED`

#### DB-06 — Unit of Work sem vazar `IClientSessionHandle`
- **Estado atual:** `NOT_STARTED`. **Proposta:** `IUnitOfWork.ExecuteAsync(delegate)` na Application, com a sessão resolvida por um acessor interno da Infrastructure.
- **Teste:** rollback do agregado quando a escrita de auditoria falha. · **Status:** `NOT_STARTED`

#### DB-07 — `AuditLog` e `ProjectSnapshot` append-only
- **Estado atual:** `PARTIAL` por convenção. **Proposta:** repositórios de escrita expõem apenas append; nenhuma rota de update ou delete.
- **Teste:** ausência de operação de update e delete nesses repositórios. · **Status:** `PARTIAL`

---

### 4.8 MOBILE_INTEGRATION

#### MOB-01 — Base URL por ambiente
- **Estado atual:** `NOT_STARTED` (achado A8). **Proposta:** `app.config.ts` com `extra` e variáveis por perfil EAS. · **Status:** `NOT_STARTED`

#### MOB-02 — Refresh transparente com single-flight
- **Estado atual:** `NOT_STARTED` (achado A8). **Proposta:** interceptor com promessa compartilhada; a falha limpa a sessão.
- **Teste:** múltiplos 401 concorrentes disparam um único refresh. · **Status:** `NOT_STARTED`

#### MOB-03 — Jornada Operator completa
- **Estado atual:** `PARTIAL`. **Evidência:** 3 telas (`MyIdeas`, `SubmitIdea`, `IdeaDetail`).
- **Gap:** Home, estratégia vigente, edição e exclusão de Draft, comentários, pontos e notificações. · **Status:** `PARTIAL`

#### MOB-04 — Jornada Manager completa
- **Estado atual:** `PARTIAL`. **Evidência:** 4 telas. **Gap:** filtros, comparação, scoring, priorização, copiloto, progresso, etapa, resultados e notificações. · **Status:** `PARTIAL`

#### MOB-05 — Jornada Leadership completa
- **Estado atual:** `PARTIAL`. **Evidência:** 1 tela (`DashboardScreen`). **Gap:** gráficos, tendências, risco, ranking, insights, CRUD de estratégia, histórico e campanhas. · **Status:** `PARTIAL`

#### MOB-06 — Compartilhado (perfil, notificações, estratégias, logout)
- **Estado atual:** `PARTIAL`. **Gap:** só existe login e logout. · **Status:** `PARTIAL`

#### MOB-07 — Charts
- **Estado atual:** `NOT_STARTED`. **Bloqueio conhecido:** achado A14. **Proposta:** spike de compatibilidade antes de fixar a biblioteca. · **Status:** `NOT_STARTED`

#### MOB-08 — Loading, empty e error states com retry
- **Estado atual:** `PARTIAL`. · **Status:** `PARTIAL`

#### MOB-09 — Interface em pt-BR
- **Estado atual:** `NOT_STARTED` (achado A9). · **Status:** `NOT_STARTED`

#### MOB-10 — API client tipado com ProblemDetails
- **Estado atual:** `PARTIAL`. **Gap:** os erros viram `Error` genérico; sem cancelamento nem tratamento por status. · **Status:** `PARTIAL`

---

### 4.9 EXTERNAL_SERVICE

#### EXT-01 — Push via OneSignal
- **Estado atual:** `NOT_STARTED`. **Dependência externa:** credenciais OneSignal e FCM.
- **Proposta:** cliente server-side e plugin Expo; a validação live é marcada como pendente se faltar credencial. · **Status:** `NOT_STARTED`

#### EXT-02 — Central de notificações persistida
- **Estado atual:** `NOT_STARTED`. **Proposta:** coleção `notifications` com read/unread e deep link. · **Status:** `NOT_STARTED`

#### EXT-03 — Outbox e HostedService
- **Estado atual:** `NOT_STARTED`. **Proposta:** `notification_outbox` gravado na mesma transação do domínio; o worker despacha fora dela. · **Status:** `NOT_STARTED`

#### EXT-04 — Retry, backoff, dead-letter e idempotência
- **Estado atual:** `NOT_STARTED`. **Teste:** falha transitória reprocessa; falha permanente vai para dead-letter; o reprocesso não duplica. · **Status:** `NOT_STARTED`

#### EXT-05 — Timeout, cancellation e circuit breaker
- **Estado atual:** `NOT_STARTED`. **Proposta:** resiliência do .NET 8 sobre `IHttpClientFactory`. · **Status:** `NOT_STARTED`

---

### 4.10 OBSERVABILITY

| ID | Requisito | Estado | Gap | Status |
|---|---|---|---|---|
| OBS-01 | Serilog estruturado | O baseline usa o logging padrão | Sem log estruturado nem enriquecimento | `NOT_STARTED` |
| OBS-02 | OpenTelemetry Traces | Ausente | ASP.NET Core, HttpClient e Mongo | `NOT_STARTED` |
| OBS-03 | OpenTelemetry Metrics | Ausente | Métricas técnicas e de negócio | `NOT_STARTED` |
| OBS-04 | `/health/live` e `/health/ready` | Ausente | — | `NOT_STARTED` |
| OBS-05 | Correlation e trace id | Ausente | Propagar e devolver no ProblemDetails | `NOT_STARTED` |
| OBS-06 | Métricas de domínio (`flow_*`) | Ausente | Sem labels de alta cardinalidade | `NOT_STARTED` |
| OBS-07 | OTLP configurável e opcional | Ausente | A ausência de collector não pode derrubar a API | `NOT_STARTED` |

---

### 4.11 SECURITY

| ID | Requisito | Estado | Evidência / Gap | Status |
|---|---|---|---|---|
| SEC-01 | ProblemDetails RFC7807 | Implementado | `Middleware/ExceptionHandlingMiddleware.cs` | `PARTIAL` |
| SEC-02 | Validação de entrada | Parcial | FluentValidation referenciado sem uso (achado A5) | `PARTIAL` |
| SEC-03 | Autorização por recurso | Parcial | Verificar leitura de ideia de terceiros em `GetIdeaByIdQueryHandler` | `PARTIAL` |
| SEC-04 | CORS explícito | Ausente | Sem `AddCors` em `Program.cs` | `NOT_STARTED` |
| SEC-05 | Rate limiting | Ausente | Necessário em auth e nos endpoints de IA | `NOT_STARTED` |
| SEC-06 | Secrets por variável de ambiente | Parcial | `appsettings.json` traz segredo placeholder | `PARTIAL` |
| SEC-07 | Validação do segredo JWT | Parcial | Só checa o prefixo `CHANGE-THIS` fora de Development | `PARTIAL` |
| SEC-08 | Refresh seguro, expiração, revogação e rotação | Parcial | Ver AUTH-06 | `PARTIAL` |
| SEC-09 | Logs sem segredos | Parcial | Sem redação explícita de chave e JWT | `PARTIAL` |

---

### 4.12 AI_PLUS

| ID | Requisito | Estado | Proposta | Status |
|---|---|---|---|---|
| AI-01 | Contratos neutros na Application | Ausente | `IInnovationAssistant` e `IExecutiveInsightService` | `NOT_STARTED` |
| AI-02 | Copiloto contextual com structured output | Ausente | Comparação, riscos, trade-offs e justificativas | `NOT_STARTED` |
| AI-03 | Function calling sobre dados autorizados | Ausente | `getCurrentStrategies`, `getIdeasUnderReview`, `compareIdeas` e afins | `NOT_STARTED` |
| AI-04 | ProjectDraft com confirmação humana | Ausente | O modelo nunca escreve no banco | `NOT_STARTED` |
| AI-05 | Insights executivos | Ausente | `POST /dashboard/insights` estruturado | `NOT_STARTED` |
| AI-06 | Evidência obrigatória, sem inventar métrica | Ausente | Deve declarar evidência insuficiente | `NOT_STARTED` |
| AI-07 | Timeout, cancellation, rate limit e resiliência | Ausente | Sem retry cego em POST não idempotente | `NOT_STARTED` |
| AI-08 | Telemetria de latência, sucesso, tokens e modelo | Ausente | Nunca logar chave, JWT ou conteúdo sensível | `NOT_STARTED` |
| AI-09 | Governança de execuções | Ausente | Coleção `assistant_runs` com solicitante, modelo, resultado e aceite | `NOT_STARTED` |
| AI-10 | `GEMINI_API_KEY` apenas server-side | Ausente | Proibido no mobile | `NOT_STARTED` |

---

### 4.13 APK

| ID | Requisito | Estado | Observação | Status |
|---|---|---|---|---|
| APK-01 | `eas.json` com profile APK | Ausente | `preview` ou `internal` gerando `.apk` | `NOT_STARTED` |
| APK-02 | `expo-doctor` e `tsc --noEmit` limpos | Não executado no baseline | Gate de qualidade | `NOT_STARTED` |
| APK-03 | Build Android | Ausente | Depende de credencial EAS | `NOT_STARTED` |
| APK-04 | Validação do APK em dispositivo | Ausente | Documentar honestamente se bloqueado por credencial | `NOT_STARTED` |

---

### 4.14 DOCUMENTATION

| ID | Arquivo | Estado | Status |
|---|---|---|---|
| DOC-01 | `docs/sprint-2/compliance-matrix.md` | Este documento | `IMPLEMENTED` |
| DOC-02 | `docs/sprint-2/architecture.md` | Ausente | `NOT_STARTED` |
| DOC-03 | `docs/sprint-2/endpoints.md` | Ausente | `NOT_STARTED` |
| DOC-04 | `docs/sprint-2/data-model.md` | Ausente | `NOT_STARTED` |
| DOC-05 | `docs/sprint-2/observability.md` | Ausente | `NOT_STARTED` |
| DOC-06 | `docs/sprint-2/ai-integration.md` | Ausente | `NOT_STARTED` |
| DOC-07 | `docs/sprint-2/demo-script.md` | Ausente | `NOT_STARTED` |
| DOC-08 | `docs/sprint-2/deployment.md` | Ausente | `NOT_STARTED` |
| DOC-09 | `docs/sprint-2/delivery-checklist.md` | Ausente | `NOT_STARTED` |
| DOC-10 | `README.md` atualizado para Mongo, Docker, demo e APK | Descreve SQL Server | `PARTIAL` |

---

### 4.15 DELIVERABLES

| ID | Requisito | Estado | Status |
|---|---|---|---|
| DEL-01 | `dist/backend/` | Ausente | `NOT_STARTED` |
| DEL-02 | `dist/mobile/` | Ausente | `NOT_STARTED` |
| DEL-03 | `dist/presentation-assets/` | Ausente | `NOT_STARTED` |
| DEL-04 | Export de `openapi.json` no pipeline | Ausente | `NOT_STARTED` |
| DEL-05 | Dockerfile, compose com replica set e deploy Dokploy/Traefik | Ausente | `NOT_STARTED` |

---

## 5. Riscos de regressão identificados

| # | Risco | Probabilidade | Impacto | Mitigação |
|---|---|---|---|---|
| R1 | Perda de atomicidade entre agregado, `AuditLog` e `ProjectSnapshot` ao trocar o change tracking do EF por escrita explícita. | Alta | Crítico | `IUnitOfWork` transacional e teste de rollback com falha injetada. |
| R2 | Stores de Identity escritos à mão divergirem do contrato e quebrarem login e papéis. | Média | Crítico | Implementar só os contratos usados; testes de integração contra Mongo real. |
| R3 | Handlers dependerem implicitamente do change tracking do EF (mutação sem `Update`). | Alta | Alto | Revisão handler a handler; repositórios com `Update` explícito. |
| R4 | Testes de integração hoje acoplados ao EF InMemory. | Certa | Alto | Migrar a factory para Testcontainers com replica set. |
| R5 | Conflito de peer dependency dos gráficos no Expo 54 (achado A14). | Alta | Médio | Spike antes de adotar; alternativa em `react-native-svg` 15.12.1, já suportado pelo SDK 54. |
| R6 | Indisponibilidade do Gemini derrubar o fluxo principal. | Média | Alto | FlowScore independente do modelo; timeout, circuit breaker e degradação previsível. |
| R7 | Ausência de credenciais OneSignal e EAS impedir a validação live. | Alta | Médio | Implementar e testar por contrato, marcando a validação live como pendente, sem simular sucesso. |
| R8 | Docker parado no ambiente bloquear testes e imagem (achado A13). | Em resolução | Alto | Daemon iniciado no início da Sprint. |
| R9 | Semântica de `DateTimeOffset` no Mongo (serialização e comparação). | Média | Alto | Serializer explícito e testes de round-trip. |
| R10 | `decimal` no Mongo exigir `Decimal128` para não perder precisão financeira. | Alta | Alto | Representação explícita e testes de precisão de ROI. |

---

## 6. Evidências da Fase 1 — migração para MongoDB

Executado em 09/09/2026 contra MongoDB 8.0.30 em replica set `rs0` de nó único.

### 6.1 Resultado dos testes

```text
Flow.Domain.Tests          124 testes  0 falhas
Flow.Application.Tests      10 testes  0 falhas
Flow.Integration.Tests      54 testes  0 falhas   (MongoDB real)
--------------------------------------------------
Total                      188 testes  0 falhas
dotnet build Flow.sln       0 erros    0 avisos
```

O baseline tinha 121 testes contra EF InMemory. Os 54 testes de integração agora rodam
contra um MongoDB real e descartável, com transações reais.

### 6.2 Requisitos que passam a `VERIFIED`

| ID | Evidência |
|---|---|
| DB-01 | Nenhuma referência a EF Core em `src/`. `ApplicationDbContext`, configurations e migrations removidos. |
| DB-02 | `MongoDB.Driver` 3.11.1, `MongoClient` singleton em `Flow.Infrastructure/DependencyInjection.cs`. |
| DB-03 | `docs/sprint-2/data-model.md` escrito antes do código, a partir dos access patterns. |
| DB-04 | `MongoIndexInitializer` cria 31 índices de forma idempotente no startup. |
| DB-05 | `TransactionalIntegrityTests` exercita transação multi-documento real. |
| DB-06 | `IUnitOfWork.ExecuteAsync` na Application; `IClientSessionHandle` não aparece em nenhuma assinatura da camada. |
| DB-07 | `IAuditLogRepository` e `IProjectSnapshotRepository` só expõem append e leitura. |
| AUTH-01..05 | `AuthTests` cobre registro, login, papéis, 401 e a matriz 403 por perfil. |
| AUTH-06 | `Refresh_ReusingARotatedToken_IsRejectedAndKillsTheChain` e `RefreshTokens_AreStoredOnlyAsHashes`. |
| AUTH-07 | `DemoDataSeeder` idempotente sob `SEED_DEMO_DATA`; segunda execução não duplica. |
| AUTH-08 | `MongoUserStore` e `MongoRoleStore` sobre os contratos oficiais do Identity, sem pacote comunitário. |
| STR-01..09 | `GuidelinesController` com CRUD, `current`, histórico e filtros; `StrategicGuidelineTests` cobre a vigência derivada. |
| IDEA-01..10 | `InnovationPipelineTests` cobre draft, edição, exclusão, submissão, score, FlowScore, comparação e autorização por recurso. |
| PRJ-01..07 | `ProjectAndResultTests` cobre a máquina de estados, stage, progresso, risco e snapshots. |
| RES-01..06 | Estimated e Actual independentes, ROI, produtividade, horas e qualidade, com precisão `Decimal128` verificada. |
| DASH-01..06 | `DashboardTests` cobre base vazia e base realista; `DashboardCompositionTests` cobre a aritmética de borda. |
| SEC-01, SEC-02 | ProblemDetails com `traceId`; `ValidationBehavior` ativo devolvendo 422. |
| SEC-03 | Autorização por recurso verificada em `Idea_OfAnotherOperator_IsNotReadable`. |
| SEC-08 | Rotação, revogação e hash do refresh token verificados. |
| OBS-04 | `/health/live` e `/health/ready` respondendo, com o Mongo como dependência de readiness. |

### 6.3 Defeitos encontrados e corrigidos durante a fase

Registrados porque são exatamente o tipo de regressão que uma migração introduz em silêncio.

| # | Defeito | Como apareceu | Correção |
|---|---|---|---|
| D1 | `MapIdMember` falhava para entidades que herdam `Id` de `BaseEntity`. | API não subia. | Registrar o class map de `BaseEntity`; a convenção do driver resolve o id por herança. |
| D2 | `UserManager` normaliza o nome do papel antes de chegar ao store, então `LEADERSHIP` era persistido. `ClaimsPrincipal.IsInRole` compara valor com ordinal sensível a caso, e **todo** `[Authorize(Roles = ...)]` devolvia 403. | Login funcionava, mas `GET /dashboard/summary` devolvia 403 para Leadership. | O store resolve o nome normalizado para o nome canônico do papel antes de gravar. |
| D3 | `["completed"] = 0` em projeção de inclusão era interpretado como exclusão pelo MongoDB. | `GET /dashboard/summary` devolvia 500. | Envolver em `$literal`. |
| D4 | O seeder só retrodatava `createdAt`, então tempo médio de conclusão e dias bloqueado ficavam em zero. | Dashboard com KPI zerado apesar de dados populados. | Retrodatar também `startDate`, `completedAt`, `blockedSince` e distribuir auditoria e snapshots ao longo do período. |
| D5 | Nome de banco de teste com 75 caracteres excedia o limite de 63 do MongoDB. | Toda a suíte de integração falhava na criação de índices. | Encurtar os identificadores gerados. |

### 6.4 Mudanças de contrato HTTP

| Situação | Antes | Agora | Motivo |
|---|---|---|---|
| Falha de validação | 400 | **422** | Distinguir entrada malformada de conflito de estado, como o brief pede para o tratamento no mobile. |
| `DomainException` | 400 | **409** | Depois da validação de entrada, o que resta são transições inválidas, que são conflito de estado. |
| Erros | sem `traceId` | `traceId` no ProblemDetails | Liga o erro reportado ao log, ao trace e ao `correlationId` da auditoria. |

---

## 7. Histórico de atualização

| Data | Fase | Alteração |
|---|---|---|
| 09/09/2026 | Fase 0 | Auditoria inicial, baseline, pesquisa de versões e criação da matriz. |
| 09/09/2026 | Fase 1 | Migração integral para MongoDB, Identity sobre Mongo, expansão de domínio, dashboard agregado, seeds de demonstração e suíte de 188 testes. |
