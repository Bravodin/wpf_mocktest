# ADR-0003 · Um projeto de contratos compartilhado, com as entidades como formato do JSON

- **Status:** Aceita
- **Data:** 2026-11-22

## Contexto

API e WPF precisam concordar sobre o formato dos dados e sobre as operações. Antes, os modelos, as interfaces
(`ICustomerService` etc.) e os DTOs (`NewSale`, `SaleFilter`, `DashboardSummary`...) viviam no `ErpMock.Core`,
junto com o EF Core e o acesso ao banco.

## Decisão

Criar o **`ErpMock.Contracts`** (`net10.0`, sem dependências) com:

- os modelos (`Customer`, `Product`, `Sale`, `SaleItem`), que já não tinham nada de EF (o mapeamento é fluente no `AppDbContext`);
- as interfaces dos serviços e os DTOs;
- o que os dois lados usam igual: `BusinessException`, `DocumentValidator` (CPF/CNPJ), títulos e CSV dos relatórios,
  e as opções de JSON (`ErpJson`: camelCase, enums como texto).

O WPF referencia só o Contracts (e o cliente HTTP), nunca o Core.

**As entidades são o próprio formato do JSON**, em vez de DTOs separados. As navegações inversas, que existem só para o EF
(`Customer.Sales`, `SaleItem.Sale`), levam `[JsonIgnore]` para não criar ciclos.

**Relatórios:** o `DataTable` não é serializável pelo System.Text.Json. Ele trafega como `ReportData` (título,
colunas com tipo, linhas) e é remontado igual no cliente. Um teste compara a tabela da API com a do serviço, célula a célula.

## Alternativas consideradas

- **DTOs próprios para a API**, separados das entidades. É o mais correto quando o modelo de domínio e o contrato
  público evoluem em ritmos diferentes, ou quando há dados que não podem sair (custo, auditoria). Aqui custaria reescrever
  todas as ViewModels e bindings para um ganho que o mock ainda não precisa. Fica como o caminho natural se a API ganhar
  clientes externos.
- **Gerar o cliente a partir do OpenAPI** (NSwag, Kiota). Faz sentido para consumidores em outras linguagens. Entre dois
  projetos .NET do mesmo repositório, compartilhar os tipos é mais simples e pega quebras de contrato na compilação.

## Consequências

- Uma mudança num modelo muda o contrato da API: os testes de ida e volta (API + cliente) existem para pegar isso.
- Propriedades calculadas (`Total`, `Margin`, `IsLowStock`) vão no JSON e são recalculadas do lado do cliente.
