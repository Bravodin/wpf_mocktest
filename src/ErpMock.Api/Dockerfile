# Imagem da API. O contexto do build é a raiz do repositório (ver docker-compose.yml):
#   docker build -f src/ErpMock.Api/Dockerfile -t erpmock-api .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# O global.json fica de fora de propósito: ele fixa a versão do SDK do Windows (10.0.404),
# e a imagem traz a sua própria versão 10.0.

# Restore primeiro, só com os .csproj: a camada fica em cache enquanto as dependências não mudam.
COPY src/ErpMock.Contracts/ErpMock.Contracts.csproj src/ErpMock.Contracts/
COPY src/ErpMock.Core/ErpMock.Core.csproj src/ErpMock.Core/
COPY src/ErpMock.Api/ErpMock.Api.csproj src/ErpMock.Api/
RUN dotnet restore src/ErpMock.Api/ErpMock.Api.csproj

COPY src/ErpMock.Contracts/ src/ErpMock.Contracts/
COPY src/ErpMock.Core/ src/ErpMock.Core/
COPY src/ErpMock.Api/ src/ErpMock.Api/
RUN dotnet publish src/ErpMock.Api/ErpMock.Api.csproj -c Release -o /app --no-restore

# Runtime em Debian (com ICU e tzdata: o ERP usa a cultura pt-BR e o fuso de São Paulo).
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "ErpMock.Api.dll"]
