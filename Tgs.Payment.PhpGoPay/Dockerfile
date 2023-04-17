#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
ARG BuildConfiguration=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Tgs.Payment.PhpGoPay/Tgs.Payment.PhpGoPay.csproj", "Tgs.Payment.PhpGoPay/"]
COPY ["PhpGoPay.Core/PhpGoPay.Core.csproj", "PhpGoPay.Core/"]
COPY ["PhpGoPay.Client/PhpGoPay.Client.csproj", "PhpGoPay.Client/"]
RUN dotnet restore "Tgs.Payment.PhpGoPay/Tgs.Payment.PhpGoPay.csproj"
COPY . .
WORKDIR "/src/Tgs.Payment.PhpGoPay"
RUN dotnet build "Tgs.Payment.PhpGoPay.csproj" -c $BuildConfiguration -o /app/build

FROM build AS publish
ARG BuildConfiguration
RUN dotnet publish "Tgs.Payment.PhpGoPay.csproj" -c $BuildConfiguration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Tgs.Payment.PhpGoPay.dll"]