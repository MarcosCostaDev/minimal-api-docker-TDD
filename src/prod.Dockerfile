﻿#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build

WORKDIR /repo
RUN apt-get install git && \
	git clone -b release https://github.com/MarcosCostaDev/minimal-api-docker-TDD .

WORKDIR /src
RUN mv ../repo/src/RinhaBackEnd RinhaBackEnd/

RUN dotnet restore "RinhaBackEnd/RinhaBackEnd.csproj"

WORKDIR "/src/RinhaBackEnd"
RUN dotnet build "RinhaBackEnd.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RinhaBackEnd.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RinhaBackEnd.dll"]