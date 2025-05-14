FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
USER $APP_UID
WORKDIR /app

# build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION
WORKDIR /src
COPY ["Olmolo.csproj", "."]
RUN dotnet restore "./Olmolo.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./Olmolo.csproj" -c $BUILD_CONFIGURATION -o /app/build

# publish
FROM build AS publish
ARG PUBLISH_RUNTIME
ARG VERSION
ARG BUILD_CONFIGURATION
RUN dotnet publish "./Olmolo.csproj" -c $BUILD_CONFIGURATION -r $PUBLISH_RUNTIME -o /app/publish -p:PublishSingleFile=true --self-contained true /p:AssemblyVersion=$VERSION /p:Version=$VERSION

# final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish/Olmolo .
ENTRYPOINT ["./Olmolo"]