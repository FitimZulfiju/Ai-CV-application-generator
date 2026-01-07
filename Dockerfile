# Use the official ASP.NET Core SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["AiCV.Web/AiCV.Web.csproj", "AiCV.Web/"]
COPY ["AiCV.Application/AiCV.Application.csproj", "AiCV.Application/"]
COPY ["AiCV.Domain/AiCV.Domain.csproj", "AiCV.Domain/"]
COPY ["AiCV.Infrastructure/AiCV.Infrastructure.csproj", "AiCV.Infrastructure/"]
RUN dotnet restore "AiCV.Web/AiCV.Web.csproj"

# Copy the rest of the source code
COPY . .
WORKDIR "/src/AiCV.Web"
RUN dotnet build "AiCV.Web.csproj" -c Release -o /app/build

FROM build AS publish
ARG BUILD_VERSION=1.0.0
RUN dotnet publish "AiCV.Web.csproj" -c Release -o /app/publish /p:Version=${BUILD_VERSION}

# Use the official ASP.NET Core runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 80
EXPOSE 443
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AiCV.Web.dll"]
