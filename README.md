# ERP Mock — WPF + API / .NET 10

Mini-ERP para estudo/portfólio: cadastro de **clientes** e **produtos**, **vendas** com baixa de estoque,
**dashboard** e **relatórios** (CSV e impressão/PDF).

As regras de negócio rodam numa **API HTTP** (ASP.NET Core, roda em Linux e Docker) e o app **WPF** é só a interface.

```
ErpMock.App (WPF, Windows) ──HTTP/JSON──► ErpMock.Api (Windows / Linux / Docker) ──► PostgreSQL ou SQLite
        só UI                                  regras de negócio (ErpMock.Core)
```

## Por que uma API

A grande vantagem é que **o WPF deixa de ser o único jeito de usar o ERP**. As regras ficam num lugar só,
e qualquer outro consumidor pode chamá-las por HTTP sem reimplementar nada:

```
                      ┌─► App WPF (Windows) ........ o que existe hoje
                      ├─► App mobile (vendedor externo lançando pedidos, consultando estoque)
ErpMock.Api  ◄──HTTP──┼─► Front web (Blazor, React...) acessível de qualquer navegador
 (regras)             ├─► Integrações: e-commerce, nota fiscal, BI/planilhas
                      └─► Outros sistemas da empresa
```

- **Mesmas regras para todos:** uma venda feita pelo app mobile baixa o estoque e valida CPF/CNPJ
  exatamente como no desktop, porque é o mesmo código, rodando na API.
- **Dados compartilhados:** todos os consumidores veem o mesmo banco, em tempo real.
- **Contrato publicado:** o documento OpenAPI (`/openapi/v1.json`) permite gerar clientes em outras linguagens
  (Kotlin, Swift, TypeScript) para quem não usa .NET.
- **Evolução independente:** dá para atualizar a regra na API sem reinstalar o app em cada máquina, ou trocar o
  front (por exemplo, WPF → web) sem tocar nas regras.

Antes de abrir para consumidores externos falta **autenticação** (ver [Limitações](#limitações-conhecidas-de-propósito-é-um-mock)):
hoje a API aceita qualquer chamada.

## Rodando

**1. Suba a API** — escolha um dos dois:

```bash
# Com Docker: API + PostgreSQL (http://localhost:8080)
docker compose up -d --build

# Sem Docker: API + SQLite num arquivo local (src/ErpMock.Api/erp.db)
dotnet run --project src/ErpMock.Api
```

Na primeira subida a API cria o banco e popula com dados fictícios pt-BR (Bogus).
Para recomeçar do zero: `docker compose down -v`, ou apague o `erp.db`.

**2. Abra o app** (Windows):

```powershell
dotnet run --project src/ErpMock.App
```

O app procura a API em `http://localhost:8080/`. Para outro endereço, edite `ErpApi:BaseUrl` em
`src/ErpMock.App/appsettings.json` ou defina a variável de ambiente `ErpApi__BaseUrl`.
Se a API não estiver no ar, o app avisa e diz como subir.

**Testes:**

```bash
dotnet test                       # Windows: a solução inteira
dotnet test tests/ErpMock.Tests   # Linux/macOS: a solução inclui o WPF, que só compila no Windows
```

## API

| Método | Rota | O que faz |
|---|---|---|
| GET | `/api/customers?search=&onlyActive=` | Lista clientes |
| POST / PUT / DELETE | `/api/customers`, `/api/customers/{id}` | Cria, altera, exclui |
| GET | `/api/products?search=&onlyActive=` · `/api/products/categories` | Lista produtos e categorias |
| POST / PUT / DELETE | `/api/products`, `/api/products/{id}` | Cria, altera, exclui |
| GET | `/api/sales?from=&to=&status=&search=` · `/api/sales/{id}` | Lista vendas, detalhe |
| POST | `/api/sales` · `/api/sales/{id}/cancel` | Fatura uma venda (baixa estoque), cancela (devolve) |
| GET | `/api/dashboard?today=` | Indicadores e gráficos |
| GET | `/api/reports/{SalesByPeriod\|SalesByCustomer\|SalesByProduct\|Inventory}?from=&to=` | Relatórios |
| GET | `/openapi/v1.json` · `/health` | Documento OpenAPI, health check |

Um erro de regra (estoque insuficiente, CPF inválido...) volta como **422** em ProblemDetails, com a mensagem
em `detail`. O app mostra essa mesma mensagem.

## Plataformas

| Projeto | Windows | Linux / macOS / Docker |
|---|---|---|
| `ErpMock.App` (WPF, `net10.0-windows`) | ✅ | ❌ — WPF só roda no Windows |
| `ErpMock.Api`, `ErpMock.Core`, `ErpMock.Contracts`, `ErpMock.Client` (`net10.0`) | ✅ | ✅ |
| `ErpMock.Tests` (`net10.0`) | ✅ | ✅ |

- Só a interface depende do Windows. A API roda em qualquer sistema com o .NET 10 e na imagem Docker (Linux).
- Fora do Docker, no Linux, a cultura `pt-BR` (CSV e nomes de meses) vem do **ICU**: instale `libicu`
  (já vem na maioria das distros) e não use `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1`.
- **Verificado até agora:** build, testes e API + app no Windows (API com SQLite). A imagem Docker e o PostgreSQL
  ainda não foram executados de verdade. O SQL do PostgreSQL é verificado offline pelos testes (`PostgresModelTests`).

## Estrutura

```
src/ErpMock.Contracts   Modelos, interfaces dos serviços e DTOs — o contrato entre API e clientes (sem dependências)
src/ErpMock.Core        Regras de negócio, DbContext (SQLite ou PostgreSQL) e seed
src/ErpMock.Api         ASP.NET Core Minimal API + Dockerfile
src/ErpMock.Client      Clientes HTTP que implementam as interfaces de Contracts
src/ErpMock.App         WPF: Views, ViewModels, estilos, converters, serviços de UI (diálogo, impressão)
tests/ErpMock.Tests     Regras de negócio (SQLite em memória) + API e cliente juntos (WebApplicationFactory)
docs/adr                Decisões de arquitetura
docker-compose.yml      API + PostgreSQL
```

## Stack

| Camada | Tecnologia |
|---|---|
| UI | WPF (.NET 10), estilos/templates próprios, sem biblioteca de componentes |
| MVVM | CommunityToolkit.Mvvm (`[ObservableProperty]`, `[RelayCommand]`, `ObservableValidator`) |
| DI / bootstrap | `Microsoft.Extensions.Hosting` (Generic Host) |
| API | ASP.NET Core Minimal API, ProblemDetails, OpenAPI |
| Dados | EF Core 10 + PostgreSQL (Npgsql) ou SQLite, `IDbContextFactory` |
| Infra | Docker (imagem multi-stage) + docker-compose |
| Testes | xUnit, SQLite em memória, `WebApplicationFactory` |

## Decisões de arquitetura

Registradas em [`docs/adr`](docs/adr/README.md):

1. [Regras de negócio numa API HTTP; o WPF fica só com a interface](docs/adr/0001-regras-de-negocio-em-api.md)
2. [PostgreSQL no Docker; SQLite como padrão para rodar sem instalar nada](docs/adr/0002-postgresql-no-docker-sqlite-local.md)
3. [Um projeto de contratos compartilhado, com as entidades como formato do JSON](docs/adr/0003-projeto-de-contratos.md)
4. [O cliente HTTP implementa as mesmas interfaces dos serviços; erro de regra vira 422](docs/adr/0004-cliente-http-com-as-mesmas-interfaces.md)

## Pontos de design

- **Mesmas interfaces, outro transporte**: as ViewModels dependem de `ICustomerService`, `ISaleService`...
  Antes eram os serviços locais; agora são clientes HTTP (`ErpMock.Client`). As telas não mudaram.
- **Testes de ida e volta**: a API real roda em memória e é chamada pelo mesmo cliente que o WPF usa.
  Relatórios e dashboard são comparados com o resultado direto dos serviços, célula a célula.
- **Navegação ViewModel-first**: `MainViewModel.CurrentPage` + `DataTemplate DataType=...` em `App.xaml`
  escolhe a View. Nenhum ViewModel conhece Views.
- **`CrudPageViewModel<TModel, TEditor>`**: base genérica para telas de cadastro (busca com debounce,
  novo/editar/excluir, painel de edição). Clientes e Produtos só implementam consulta/persistência.
- **Validação**: `ObservableValidator` + DataAnnotations → `INotifyDataErrorInfo`, com `ErrorTemplate`
  customizado. CPF/CNPJ validados pelo dígito verificador (`CustomValidation`), revalidando ao trocar PF/PJ.
  A API valida de novo: a regra que vale é a do servidor.
- **`IDbContextFactory`** em vez de um DbContext de vida longa: cada operação abre e descarta seu contexto.
- **Regras de venda** em transação: valida estoque, agrupa itens repetidos, baixa estoque;
  cancelamento devolve ao estoque. Cobertas por testes.
- **Diálogos e impressão atrás de interfaces** (`IDialogService`, `IPrintService`) → ViewModels testáveis.
- **Gráficos sem biblioteca**: `ItemsControl` + `ScaleTransform` ligado à razão (0..1) calculada no ViewModel.
- **Relatórios como `DataTable`**: um formato só para exibir (`DataGrid` com colunas geradas),
  exportar CSV (`;` e vírgula decimal, UTF-8 com BOM para o Excel) e imprimir (`FlowDocument`).
  Na API trafegam como `ReportData` (colunas tipadas + linhas) e são remontados iguais no app.
- **Cultura pt-BR** aplicada globalmente (`FrameworkElement.LanguageProperty`) → `StringFormat=C` vira R$.

## Limitações conhecidas (de propósito, é um mock)

- Sem autenticação/usuários; a API aceita qualquer chamada.
- Sem migrations (`EnsureCreated`): o banco é descartável. Ver [ADR-0002](docs/adr/0002-postgresql-no-docker-sqlite-local.md).
- Dashboard e relatórios agregam em memória (o SQLite não agrega `decimal` em SQL); ok para o volume do mock.
- A senha do PostgreSQL no `docker-compose.yml` é só para desenvolvimento local.
