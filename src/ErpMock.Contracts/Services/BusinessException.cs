namespace ErpMock.Contracts.Services;

/// <summary>Erro de regra de negócio, com mensagem pronta para exibir ao usuário.</summary>
public class BusinessException(string message) : Exception(message);
