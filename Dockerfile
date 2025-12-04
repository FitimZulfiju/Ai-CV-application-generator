# Use the official ASP.NET Core SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["WebCV.Web/WebCV.Web.csproj", "WebCV.Web/"]
COPY ["WebCV.Application/WebCV.Application.csproj", "WebCV.Application/"]
COPY ["WebCV.Domain/WebCV.Domain.csproj", "WebCV.Domain/"]
COPY ["WebCV.Infrastructure/WebCV.Infrastructure.csproj", "WebCV.Infrastructure/"]
RUN dotnet restore "WebCV.Web/WebCV.Web.csproj"

# Copy the rest of the source code
COPY . .
WORKDIR "/src/WebCV.Web"
RUN dotnet build "WebCV.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WebCV.Web.csproj" -c Release -o /app/publish

# Use the official ASP.NET Core runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 80
EXPOSE 443
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebCV.Web.dll"]
