FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Paymant_Module_NEOXONLINE/Paymant_Module_NEOXONLINE.csproj", "Paymant_Module_NEOXONLINE/"]
COPY ["Payment.Application/Payment.Application.csproj", "Payment.Application/"]
COPY ["Payment.Domain/Payment.Domain.csproj", "Payment.Domain/"]
COPY ["Payment.BLL/Payment.BLL.csproj", "Payment.BLL/"]

RUN dotnet restore "Paymant_Module_NEOXONLINE/Paymant_Module_NEOXONLINE.csproj"

COPY . .
WORKDIR "/src/Paymant_Module_NEOXONLINE"
RUN dotnet build "Paymant_Module_NEOXONLINE.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Paymant_Module_NEOXONLINE.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Paymant_Module_NEOXONLINE.dll"]