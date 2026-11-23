# ADR-0002 · PostgreSQL no Docker; SQLite como padrão para rodar sem instalar nada

- **Status:** Aceita
- **Data:** 2026-11-22

## Contexto

Com a API ([ADR-0001](0001-regras-de-negocio-em-api.md)), o banco deixa de ser um arquivo por estação e passa a ser
compartilhado. Ao mesmo tempo, quem clona o repositório deve conseguir rodar o projeto mesmo sem Docker.

## Decisão

- O **`docker-compose.yml`** sobe a API e um **PostgreSQL 17**, com os dados num volume (`pgdata`).
- Sem Docker, a API usa **SQLite** num arquivo local (`src/ErpMock.Api/erp.db`), sem instalar nada.
- A escolha é por configuração: `Database:Provider` (`Sqlite` ou `Postgres`) e `ConnectionStrings:Erp`.
  O compose define as duas por variáveis de ambiente.
- O banco é criado com `EnsureCreated` e populado com dados fictícios na primeira subida da API.
  No Docker, a API tenta de novo por até ~30 s enquanto o PostgreSQL termina de subir, além do `healthcheck`.

## Detalhes que a decisão exigiu

- **Datas:** o ERP grava hora local (`DateTime.Now`). O Npgsql mapeia `DateTime` para `timestamp with time zone`,
  que só aceita UTC. No PostgreSQL as datas usam `timestamp without time zone` (convenção no `AppDbContext`,
  aplicada só quando o provedor é o Npgsql). O container roda com `TZ=America/Sao_Paulo`, senão "agora" seria UTC.
- **Cultura:** a imagem de runtime é a Debian padrão (`aspnet:10.0`), que já tem ICU. Relatórios e nomes de meses
  usam `pt-BR`, o que falharia numa imagem com globalização invariante.
- **Sem retry automático do EF** (`EnableRetryOnFailure`): ele não aceita as transações abertas pelo código
  (venda e cancelamento usam `BeginTransactionAsync`).

## Alternativas consideradas

- **Só PostgreSQL.** Mais fiel à produção, mas obriga quem clona o repositório a ter Docker para ver o app funcionando.
- **SQL Server.** Combina com o ecossistema .NET, mas a imagem é pesada e o PostgreSQL é gratuito e leve.
- **Migrations em vez de `EnsureCreated`.** São o certo quando o esquema evolui com dados em produção. No mock o banco é
  descartável (`docker compose down -v` recomeça do zero). Fica registrado como próximo passo.

## Consequências

- Dois provedores para manter compatíveis. Os testes geram o SQL do PostgreSQL offline (`PostgresModelTests`):
  o esquema e a tradução das buscas são verificados sem servidor. Rodar contra um PostgreSQL de verdade é o compose.
- O SQLite não agrega `decimal` em SQL, então dashboard e relatórios agregam em memória (como antes).
  Com o PostgreSQL daria para mover para o banco, se o volume crescer.
