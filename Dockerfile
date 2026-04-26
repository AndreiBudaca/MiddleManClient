FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY MiddleManClient/MiddleManClient.csproj MiddleManClient/
COPY MiddleManClient.Test/MiddleManClient.Test.csproj MiddleManClient.Test/
RUN dotnet restore MiddleManClient.Test/MiddleManClient.Test.csproj

COPY MiddleManClient/ MiddleManClient/
COPY MiddleManClient.Test/ MiddleManClient.Test/
RUN dotnet publish MiddleManClient.Test/MiddleManClient.Test.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "MiddleManClient.Test.dll"]
