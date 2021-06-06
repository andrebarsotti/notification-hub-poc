FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
COPY . /app
WORKDIR /app
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:5.0-buster-slim
WORKDIR /app
EXPOSE 80
EXPOSE 443
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NotificationHubPoc.dll"]