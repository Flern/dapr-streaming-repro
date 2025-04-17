FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
ARG Mode=Debug
ARG UserHome


COPY . /src/

WORKDIR /src/GrpcClient

RUN dotnet publish "GrpcClient.csproj" -c Debug --runtime linux-musl-x64 --no-self-contained -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:7.0-alpine AS base

RUN apk upgrade -U

EXPOSE 5000 7000
ENV ASPNETCORE_URLS=http://*:5000

WORKDIR /app
COPY --from=build /app/publish .

RUN chown -R guest:users /app
USER guest
ENTRYPOINT ["dotnet", "GrpcClient.dll"] 
