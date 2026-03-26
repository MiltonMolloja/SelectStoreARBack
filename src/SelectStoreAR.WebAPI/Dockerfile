FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/SelectStoreAR.WebAPI/SelectStoreAR.WebAPI.csproj",          "src/SelectStoreAR.WebAPI/"]
COPY ["src/SelectStoreAR.Application/SelectStoreAR.Application.csproj", "src/SelectStoreAR.Application/"]
COPY ["src/SelectStoreAR.Domain/SelectStoreAR.Domain.csproj",           "src/SelectStoreAR.Domain/"]
COPY ["src/SelectStoreAR.Infrastructure/SelectStoreAR.Infrastructure.csproj", "src/SelectStoreAR.Infrastructure/"]
COPY ["Directory.Build.props", "."]
COPY [".editorconfig", "."]

RUN dotnet restore "src/SelectStoreAR.WebAPI/SelectStoreAR.WebAPI.csproj"

COPY . .
WORKDIR "/src/src/SelectStoreAR.WebAPI"
RUN dotnet build "SelectStoreAR.WebAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SelectStoreAR.WebAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Crear directorio para imagenes
RUN mkdir -p /app/images/products

ENTRYPOINT ["dotnet", "SelectStoreAR.WebAPI.dll"]
