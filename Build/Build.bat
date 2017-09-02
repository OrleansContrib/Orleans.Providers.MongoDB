@echo off
dotnet build --configuration release "..\Orleans.Providers.MongoDB\Orleans.Providers.MongoDB.csproj"
dotnet pack --configuration release "..\Orleans.Providers.MongoDB\Orleans.Providers.MongoDB.csproj"