# Decisões de arquitetura (ADRs)

Cada decisão relevante fica registrada num arquivo curto: contexto, decisão, alternativas e consequências.
Quando uma decisão mudar, escreva uma ADR nova que substitui a antiga (sem reescrever a história).

| # | Decisão | Status |
|---|---|---|
| [0001](0001-regras-de-negocio-em-api.md) | Regras de negócio numa API HTTP; o WPF fica só com a interface | Aceita |
| [0002](0002-postgresql-no-docker-sqlite-local.md) | PostgreSQL no Docker; SQLite como padrão para rodar sem instalar nada | Aceita |
| [0003](0003-projeto-de-contratos.md) | Um projeto de contratos compartilhado, com as entidades como formato do JSON | Aceita |
| [0004](0004-cliente-http-com-as-mesmas-interfaces.md) | O cliente HTTP implementa as mesmas interfaces dos serviços; erro de regra vira 422 | Aceita |

Modelo para uma nova ADR: copie a 0001 e mantenha as mesmas seções.
