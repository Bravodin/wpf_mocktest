# ADR-0001 · Regras de negócio numa API HTTP; o WPF fica só com a interface

- **Status:** Aceita
- **Data:** 2026-11-22

## Contexto

Na primeira versão o app WPF fazia tudo no mesmo processo: interface, regras de negócio (`ErpMock.Core`)
e acesso ao banco SQLite em `%LOCALAPPDATA%`. Funciona para um usuário numa máquina, mas:

- cada estação tem seu próprio banco, então não há dados compartilhados entre usuários;
- a regra roda na máquina do usuário, que pode burlá-la (é só falar direto com o banco);
- qualquer outro front (web, mobile, integração) teria que reimplementar as regras;
- tudo depende do Windows, porque o WPF depende.

## Decisão

As regras de negócio passam a rodar numa **API HTTP** (`ErpMock.Api`, ASP.NET Core Minimal API, `net10.0`).
O WPF (`ErpMock.App`) vira só interface e chama a API por HTTP. Ele não referencia mais o Core, o EF Core nem o banco.

```
WPF (Windows) ──HTTP/JSON──► ErpMock.Api (Windows/Linux/Docker) ──► PostgreSQL ou SQLite
   só UI                        regras (ErpMock.Core)
```

## Alternativas consideradas

- **Manter tudo no processo do WPF.** Mais simples e funciona offline, mas mantém os problemas acima.
  Continua sendo a escolha certa para um app de usuário único e offline; aqui o objetivo é um ERP multiusuário.
- **gRPC em vez de REST/JSON.** É mais eficiente, mas o REST é mais fácil de inspecionar (curl, navegador, OpenAPI)
  e de consumir por qualquer front. O volume de dados do mock não justifica o gRPC.
- **Front também multiplataforma (Avalonia, Blazor).** Fica para depois. Separar a API é o passo que vem antes de
  qualquer troca de front.

## Consequências

- A API roda em Linux e em container, e só o front depende do Windows.
- Ganha-se uma fronteira de rede: é preciso tratar a API fora do ar (mensagem amigável no app),
  timeouts e versionamento do contrato.
- A validação que melhora a experiência (CPF inválido aparecer na hora) continua no WPF. A validação que
  vale de verdade é a da API.
- Autenticação e usuários (hoje fora do escopo) passam a ter um lugar natural: a API.
