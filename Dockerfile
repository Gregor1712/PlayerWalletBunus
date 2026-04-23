FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

COPY PlayerWallet.sln .
COPY Directory.Build.props .
COPY PlayerWallet.Api/PlayerWallet.Api.csproj PlayerWallet.Api/
COPY PlayerWallet.Application/PlayerWallet.Application.csproj PlayerWallet.Application/
COPY PlayerWallet.Domain/PlayerWallet.Domain.csproj PlayerWallet.Domain/
COPY PlayerWallet.Infrastructure/PlayerWallet.Infrastructure.csproj PlayerWallet.Infrastructure/
COPY PlayerWallet.Tests/PlayerWallet.Tests.csproj PlayerWallet.Tests/
RUN dotnet restore

COPY . .
RUN dotnet publish PlayerWallet.Api/PlayerWallet.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "PlayerWallet.Api.dll"]