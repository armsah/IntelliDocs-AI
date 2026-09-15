FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/IntelliDocs.ReviewPortal/IntelliDocs.ReviewPortal.csproj", "src/IntelliDocs.ReviewPortal/"]

RUN dotnet restore "src/IntelliDocs.ReviewPortal/IntelliDocs.ReviewPortal.csproj"

COPY . .
RUN dotnet publish "src/IntelliDocs.ReviewPortal/IntelliDocs.ReviewPortal.csproj" \
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

ENTRYPOINT ["dotnet", "IntelliDocs.ReviewPortal.dll"]