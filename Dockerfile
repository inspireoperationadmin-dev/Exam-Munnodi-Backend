# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ScholarFlow.slnx ./
COPY ScholarFlow.Domain/ScholarFlow.Domain.csproj ScholarFlow.Domain/
COPY ScholarFlow.Infrastructure/ScholarFlow.Infrastructure.csproj ScholarFlow.Infrastructure/
COPY ScholarFlow.SharedKernel/ScholarFlow.SharedKernel.csproj ScholarFlow.SharedKernel/
COPY ScholarFlow.WebAPI/ScholarFlow.WebAPI.csproj ScholarFlow.WebAPI/
COPY Modules/ScholarFlow.Modules.Academic/ScholarFlow.Modules.Academic.csproj Modules/ScholarFlow.Modules.Academic/
COPY Modules/ScholarFlow.Modules.Analytics/ScholarFlow.Modules.Analytics.csproj Modules/ScholarFlow.Modules.Analytics/
COPY Modules/ScholarFlow.Modules.Examination/ScholarFlow.Modules.Examination.csproj Modules/ScholarFlow.Modules.Examination/
COPY Modules/ScholarFlow.Modules.Identity/ScholarFlow.Modules.Identity.csproj Modules/ScholarFlow.Modules.Identity/
COPY Modules/ScholarFlow.Modules.UserProfiles/ScholarFlow.Modules.UserProfiles.csproj Modules/ScholarFlow.Modules.UserProfiles/

RUN dotnet restore ScholarFlow.WebAPI/ScholarFlow.WebAPI.csproj

ARG BUILD_VERSION=1

COPY . .

RUN dotnet publish ScholarFlow.WebAPI/ScholarFlow.WebAPI.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ARG PORT=8080

ENV ASPNETCORE_URLS=http://+:${PORT:-8080}

EXPOSE 8080

ENTRYPOINT ["dotnet", "ScholarFlow.WebAPI.dll"]