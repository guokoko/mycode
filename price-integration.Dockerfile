ARG DOTNET_TAG=3.1

FROM mcr.microsoft.com/dotnet/core/sdk:$DOTNET_TAG AS build

ARG REPORT_SERVER_URL="https://report-portal.central.tech/api/v1/price-service/launch"
ARG ReportPortal_Server_Authentication_Uuid="430f4cac-c997-45cf-ae49-cdadbabc57c3"
ARG ReportPortal_launch_name="integrationtest"

COPY . /app
WORKDIR /app

# report portal config
RUN dotnet add IntegrationTests package ReportPortal.XUnit

# run test
RUN dotnet test IntegrationTests/IntegrationTests.csproj /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:Exclude=\"[*.Views*]*,[*Test*]*\";


ENTRYPOINT ["/bin/bash"]