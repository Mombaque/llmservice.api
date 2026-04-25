FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["LlmService.Api.csproj", "."]
RUN dotnet restore "LlmService.Api.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "LlmService.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LlmService.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 5002
ENV ASPNETCORE_URLS=http://+:5002
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LlmService.Api.dll"]
