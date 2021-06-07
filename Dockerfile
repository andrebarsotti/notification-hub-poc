FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build-env
WORKDIR /app

COPY . ./

RUN dotnet restore /app/src/NotificationHubPoc/NotificationHubPoc.csproj
RUN dotnet publish /app/src/NotificationHubPoc/NotificationHubPoc.csproj -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:5.0-alpine
EXPOSE 80
EXPOSE 443
WORKDIR /app
COPY --from=build-env /app/out/ .
ENTRYPOINT ["dotnet", "NotificationHubPoc.dll"]