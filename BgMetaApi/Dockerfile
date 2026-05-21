FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["BgMetaApi/BgMetaApi.csproj", "BgMetaApi/"]
RUN dotnet restore "BgMetaApi/BgMetaApi.csproj"
COPY BgMetaApi/ BgMetaApi/
WORKDIR /src/BgMetaApi
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
ENV Meta__DataDirectory=/data
EXPOSE 8080
RUN mkdir -p /data
VOLUME ["/data"]
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BgMetaApi.dll"]
