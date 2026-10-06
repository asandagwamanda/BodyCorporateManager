FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore "BodyCorporateManager.Web.csproj"
RUN dotnet publish "BodyCorporateManager.Web.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV DB_PATH=/data/bodycorporate.db

RUN mkdir -p /data
EXPOSE 8080

ENTRYPOINT ["dotnet", "BodyCorporateManager.Web.dll"]
