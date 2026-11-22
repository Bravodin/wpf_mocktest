# ERP Mock — WPF / .NET 10

Mini-ERP desktop para estudo/portfólio: cadastro de **clientes** e **produtos**, **vendas** com baixa de estoque,
**dashboard** e **relatórios** (CSV e impressão/PDF).

## Rodando

```powershell
dotnet run --project src/ErpMock.App
dotnet test
```

Na primeira execução é criado um banco SQLite em `%LOCALAPPDATA%\ErpMock\erp.db`, populado com
dados fictícios pt-BR (Bogus). Para recomeçar do zero, apague esse arquivo.

## Stack

| Camada | Tecnologia |
|---|---|
| UI | WPF (.NET 10), estilos/templates próprios, sem biblioteca de componentes |
| MVVM | CommunityToolkit.Mvvm (`[ObservableProperty]`, `[RelayCommand]`, `ObservableValidator`) |
| DI / bootstrap | `Microsoft.Extensions.Hosting` (Generic Host) |
| Dados | EF Core 10 + SQLite, `IDbContextFactory` |
| Testes | xUnit + SQLite em memória |

## Estrutura

```
src/ErpMock.Core      Modelos, DbContext, seed e serviços (regras de negócio) — sem dependência de WPF
src/ErpMock.App       WPF: Views, ViewModels, estilos, converters, serviços de UI (diálogo, impressão)
tests/ErpMock.Tests   Testes das regras de negócio
```

## Pontos de design

- **Navegação ViewModel-first**: `MainViewModel.CurrentPage` + `DataTemplate DataType=...` em `App.xaml`
  escolhe a View. Nenhum ViewModel conhece Views.
- **`CrudPageViewModel<TModel, TEditor>`**: base genérica para telas de cadastro (busca com debounce,
  novo/editar/excluir, painel de edição). Clientes e Produtos só implementam consulta/persistência.
- **Validação**: `ObservableValidator` + DataAnnotations → `INotifyDataErrorInfo`, com `ErrorTemplate`
  customizado. CPF/CNPJ validados pelo dígito verificador (`CustomValidation`), revalidando ao trocar PF/PJ.
- **`IDbContextFactory`** em vez de um DbContext de vida longa: em desktop cada operação abre e
  descarta seu contexto (sem tracking acumulado nem problemas de concorrência).
- **Regras de venda** em transação: valida estoque, agrupa itens repetidos, baixa estoque;
  cancelamento devolve ao estoque. Cobertas por testes.
- **Diálogos e impressão atrás de interfaces** (`IDialogService`, `IPrintService`) → ViewModels testáveis.
- **Gráficos sem biblioteca**: `ItemsControl` + `ScaleTransform` ligado à razão (0..1) calculada no ViewModel.
- **Relatórios como `DataTable`**: um formato só para exibir (`DataGrid` com colunas geradas),
  exportar CSV (`;` e vírgula decimal, UTF-8 com BOM para o Excel) e imprimir (`FlowDocument`).
- **Cultura pt-BR** aplicada globalmente (`FrameworkElement.LanguageProperty`) → `StringFormat=C` vira R$.

## Limitações conhecidas (de propósito, é um mock)

- SQLite não agrega `decimal` em SQL; dashboard e relatórios agregam em memória (ok para o volume do mock).
- Sem autenticação/usuários, sem migrations (`EnsureCreated`).
