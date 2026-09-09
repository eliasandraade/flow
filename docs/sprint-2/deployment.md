# Deploy

Como subir o Flow: local, em containers e em produção com Dokploy e Traefik.

---

## 1. Pré-requisitos

| Item | Versão | Observação |
|---|---|---|
| .NET SDK | 8.0 | |
| MongoDB | 8.0 | **Replica set obrigatório** |
| Node | 20+ | Apenas para o mobile |
| Docker | 24+ | Opcional para desenvolvimento; necessário para a imagem |

### Por que o replica set não é opcional

O Flow grava o agregado, a entrada de auditoria e o snapshot do projeto dentro de **uma
transação multi-documento**, e transações não existem em um `mongod` standalone. Um Mongo
avulso sobe, aceita leitura e escrita e parece funcionar — até a primeira transição de
projeto falhar.

Em desenvolvimento e em teste, um replica set de **nó único** é suficiente e é o que o
`docker-compose.yml` cria.

---

## 2. Local, sem Docker

```bash
# 1. MongoDB como replica set de nó único
mongod --replSet rs0 --dbpath ./data --port 27017 --bind_ip 127.0.0.1

# 2. iniciar o conjunto (uma única vez)
mongosh --eval 'rs.initiate({_id:"rs0", members:[{_id:0, host:"127.0.0.1:27017"}]})'
mongosh --eval 'rs.status().myState'   # 1 = PRIMARY

# 3. configuração
cp .env.example .env     # preencha JWT_SECRET_KEY

# 4. API
export JwtSettings__SecretKey="$(openssl rand -base64 48)"
export SEED_DEMO_DATA=true
export SEED_DEMO_PASSWORD='FlowDemo!2026'
dotnet run --project src/Flow.API
```

A API sobe em `http://localhost:5153`, com Swagger em `/swagger`.

No startup ela cria os índices de forma idempotente e garante os três papéis do Identity.

### Mobile

```bash
cd mobile
npm ci
npx expo start
```

A URL da API é resolvida sozinha: em desenvolvimento o app usa o host que serve o bundle,
então um aparelho na mesma rede encontra a máquina sem nenhuma edição.

---

## 3. Docker Compose

```bash
cp .env.example .env      # JWT_SECRET_KEY é obrigatório
docker compose up -d
docker compose logs -f api
```

O compose sobe dois serviços:

- **mongo** — `mongo:8.0` com `--replSet rs0`. O healthcheck executa `rs.initiate` na
  primeira subida e só reporta saudável quando o nó é PRIMARY.
- **api** — imagem multi-stage, com `depends_on: service_healthy`, de modo que a API só
  inicia quando o replica set está pronto.

### A imagem

- build em `sdk:8.0-alpine`, runtime em `aspnet:8.0-alpine`;
- restore em camada separada, então uma edição de código não reinstala pacotes;
- roda como usuário **não privilegiado**;
- `HEALTHCHECK` aponta para `/health/ready`, não `/health/live`: o orquestrador só deve
  encaminhar tráfego quando o MongoDB estiver de fato alcançável;
- `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false` e `icu-libs`, necessários para a formatação
  pt-BR de moeda e data;
- **nenhum segredo na imagem** — tudo chega por variável de ambiente.

---

## 4. Produção com Dokploy e Traefik

### 4.1 MongoDB

Provisionar um replica set. Uma instância avulsa **não serve**.

```text
mongodb://<user>:<senha>@<host>:27017/?replicaSet=rs0&authSource=admin&tls=true
```

### 4.2 Aplicação no Dokploy

1. Criar uma aplicação do tipo **Docker** apontando para o repositório;
2. Build path `/`, Dockerfile `Dockerfile`;
3. Porta interna **8080**;
4. Health check path `/health/ready`.

### 4.3 Variáveis de ambiente

```bash
ASPNETCORE_ENVIRONMENT=Production

Mongo__ConnectionString=<a string acima>
Mongo__Database=flow

JwtSettings__SecretKey=<openssl rand -base64 48>
JwtSettings__Issuer=FlowAPI
JwtSettings__Audience=FlowApp
JwtSettings__ExpiryMinutes=15
Auth__RefreshTokenDays=7

Gemini__ApiKey=<chave>
Gemini__Model=gemini-3.8-flash

OneSignal__AppId=<app id>
OneSignal__ApiKey=<rest api key>

CORS_ALLOWED_ORIGINS=https://flow.example.com
Swagger__Enabled=false
SEED_DEMO_DATA=false
```

> A API **recusa iniciar** fora de Development se `JwtSettings__SecretKey` ainda for o
> placeholder ou tiver menos de 32 bytes. É deliberado: um segredo fraco em produção é a
> falha mais fácil de cometer e a mais cara de descobrir depois.

### 4.4 HTTPS com Traefik

O Dokploy já roda Traefik. Basta associar o domínio e habilitar o certificado; o
`UseHttpsRedirection` da API cuida do restante.

```yaml
# labels equivalentes, caso configure na mão
traefik.enable: "true"
traefik.http.routers.flow.rule: "Host(`flow-api.example.com`)"
traefik.http.routers.flow.entrypoints: "websecure"
traefik.http.routers.flow.tls.certresolver: "letsencrypt"
traefik.http.services.flow.loadbalancer.server.port: "8080"
```

Como o TLS termina no Traefik, a aplicação recebe HTTP interno. Se algum dia a URL absoluta
gerada estiver errada, o ajuste é `ForwardedHeaders`, não desligar o redirecionamento.

### 4.5 Depois do deploy

```bash
curl -fsS https://flow-api.example.com/health/live
curl -fsS https://flow-api.example.com/health/ready
```

`ready` verde significa que o MongoDB respondeu e os índices existem.

---

## 5. Mobile

```bash
cd mobile

# JS puro, para conferir se o projeto empacota
npx expo export --platform android

# build nativo (exige credencial EAS)
eas build --platform android --profile preview   # APK instalável
```

Perfis em `eas.json`:

| Perfil | Saída | Uso |
|---|---|---|
| `development` | APK com dev client | Depuração com API local |
| `preview` | **APK** | Distribuição interna e instalação direta |
| `production` | **APK** | Entrega |
| `production-store` | AAB | Google Play |

A URL da API vem do perfil, por `EXPO_PUBLIC_API_URL`. Ajuste antes de gerar o build.

---

## 6. Segredos

| Segredo | Onde vive | Onde **nunca** vive |
|---|---|---|
| `JwtSettings__SecretKey` | Variável de ambiente | Repositório, imagem, log |
| `Gemini__ApiKey` | Variável de ambiente, servidor | App mobile, log, `assistant_runs` |
| `OneSignal__ApiKey` | Variável de ambiente, servidor | App mobile |
| OneSignal **App ID** | Perfil EAS | — é público por natureza |
| Senha do MongoDB | Connection string por env | Repositório |
| `SEED_DEMO_PASSWORD` | Variável de ambiente, só em demo | Produção |

`.env` está no `.gitignore`. `.env.example` traz todas as chaves com valores vazios.

---

## 7. Estado de verificação

| Item | Estado |
|---|---|
| API rodando contra MongoDB 8.0 em replica set | ✅ verificado nesta máquina |
| Criação idempotente de índices no startup | ✅ verificado |
| Health checks respondendo | ✅ verificado |
| Seed de demonstração idempotente | ✅ verificado |
| Export de `openapi.json` a partir da aplicação | ✅ verificado, 54 endpoints |
| Bundle Android do mobile | ✅ verificado, 5,59 MB Hermes |
| `expo-doctor` | ✅ 18/18 |
| **Build da imagem Docker** | ⏳ **não executado** |
| **`docker compose up`** | ⏳ **não executado** |
| **Deploy no Dokploy com HTTPS** | ⏳ **não executado** |
| **APK via EAS** | ⏳ **não executado** |

### Por que os quatro últimos estão pendentes

**Docker.** O Docker Desktop desta máquina não sobe: os processos iniciam, mas a distro
WSL `docker-desktop` permanece em `Stopped` e `docker desktop status` trava. Foram
tentadas a inicialização direta, `docker desktop start` e um restart completo com
`wsl --shutdown`. O `Dockerfile` e o `docker-compose.yml` estão escritos e revisados, mas
**não foram construídos nem executados aqui**, e não vou afirmar o contrário.

Para validar em uma máquina com Docker funcionando:

```bash
docker compose build
docker compose up -d
curl -fsS localhost:5153/health/ready
```

**Dokploy e EAS.** Dependem de credenciais que não existem neste ambiente: acesso ao
servidor Dokploy e conta EAS com credencial de assinatura Android.

O que **foi** verificado é o que sustenta os dois: a aplicação publica em Release, sobe
contra um MongoDB real, responde nos health checks, e o projeto mobile empacota para
Android. O que falta é execução com credencial, não código.
