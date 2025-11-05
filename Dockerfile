# ===========================
# Build Stage
# ===========================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY *.csproj ./

# Restore dependencies
RUN dotnet restore

# Copy the remaining source code
COPY . ./

# Build and publish the app
RUN dotnet publish -c Release -o /app/publish

# ===========================
# Runtime Stage
# ===========================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy the published output from the build stage
COPY --from=build /app/publish .

# Expose the default ASP.NET Core port
EXPOSE 80

# Start the application
ENTRYPOINT ["dotnet", "HospitalAppointmentApi.dll"]
