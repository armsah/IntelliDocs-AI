FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/IntelliDocs.Api/IntelliDocs.Api.csproj", "src/IntelliDocs.Api/"]
COPY ["src/IntelliDocs.Core/IntelliDocs.Core.csproj", "src/IntelliDocs.Core/"]
COPY ["src/IntelliDocs.Infrastructure/IntelliDocs.Infrastructure.csproj", "src/IntelliDocs.Infrastructure/"]

RUN dotnet restore "src/IntelliDocs.Api/IntelliDocs.Api.csproj"

COPY . .
RUN dotnet publish "src/IntelliDocs.Api/IntelliDocs.Api.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "IntelliDocs.Api.dll"]