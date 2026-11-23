using ErpMock.Contracts.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace ErpMock.Api;

/// <summary>
/// Erros esperados viram ProblemDetails com a mensagem em "detail", que o cliente mostra ao usuário.
/// O resto segue para o tratamento padrão (500, sem detalhes internos).
/// </summary>
internal sealed class ErrorHandler(IProblemDetailsService problems) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        (int Status, string Title, string Detail)? error = exception switch
        {
            BusinessException e => (StatusCodes.Status422UnprocessableEntity, "Regra de negócio", e.Message),
            // PUT de um registro que não existe (ou foi excluído enquanto era editado).
            DbUpdateConcurrencyException => (StatusCodes.Status404NotFound, "Não encontrado",
                "O registro não existe mais. Atualize a lista e tente de novo."),
            _ => null,
        };
        if (error is not { } known) return false;

        context.Response.StatusCode = known.Status;
        return await problems.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = { Status = known.Status, Title = known.Title, Detail = known.Detail },
        });
    }
}
