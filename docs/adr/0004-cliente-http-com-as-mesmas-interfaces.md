# ADR-0004 · O cliente HTTP implementa as mesmas interfaces dos serviços; erro de regra vira 422

- **Status:** Aceita
- **Data:** 2026-11-22

## Contexto

As ViewModels do WPF já dependiam de interfaces (`ICustomerService`, `ISaleService`...) e tratavam erros de regra
pegando `BusinessException` e mostrando a mensagem ao usuário. Mover as regras para a API
([ADR-0001](0001-regras-de-negocio-em-api.md)) não deveria obrigar a reescrever as telas.

## Decisão

- O **`ErpMock.Client`** (`net10.0`) implementa as mesmas interfaces chamando a API (`CustomerApiClient`,
  `SaleApiClient`...). No WPF, a única troca é na injeção de dependência: `AddErpCore(...)` virou `AddErpApiClient(url)`.
- **Erros:** a API devolve `BusinessException` como **422 Unprocessable Entity** em ProblemDetails
  (`application/problem+json`), com a mensagem em `detail`. O cliente converte qualquer 4xx com `detail` de volta em
  `BusinessException`, e a tela mostra a mesma mensagem de antes.
- **API fora do ar ou lenta:** o cliente lança `ApiUnavailableException` com uma mensagem que diz o endereço e como subir
  a API. O WPF mostra essa mensagem sem o prefixo "Erro inesperado".
- O cliente fica num projeto `net10.0` separado (e não dentro do WPF) para ser testado contra a API real em memória
  (`WebApplicationFactory`), em qualquer sistema operacional.
- Endereço da API: `ErpApi:BaseUrl` no `appsettings.json` do app (padrão `http://localhost:8080/`) ou a variável
  `ErpApi__BaseUrl`.

## Alternativas consideradas

- **Resultado em vez de exceção** (`Result<T>`) nas interfaces. Seria mais explícito, mas mudaria todas as ViewModels.
  O padrão com exceção já existia e continua consistente dos dois lados.
- **400 Bad Request para erro de regra.** O 400 fica para requisição malformada (JSON inválido). O 422 diz que a requisição
  é válida mas fere uma regra, e o cliente consegue distinguir as duas.

## Consequências

- As telas não mudaram (só os `using`). O que muda é a latência: cada operação é uma chamada HTTP.
- Os testes de ida e volta (`tests/ErpMock.Tests/Api`) usam o mesmo cliente que o WPF. Se a API e o cliente
  discordarem, o teste quebra antes do app.
