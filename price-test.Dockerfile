FROM 491229787580.dkr.ecr.ap-southeast-1.amazonaws.com/common/price-service-sonar:latest AS build

ARG SONAR_LOGIN="8a235a181c1a26da266f4587ffcde787ba772504"
ARG REPORT_SERVER_URL="https://report-portal.central.tech/api/v1/price-service/launch"
ARG ReportPortal_Server_Authentication_Uuid="430f4cac-c997-45cf-ae49-cdadbabc57c3"
ARG ReportPortal_launch_name="unittest"

COPY . /app
  
# report portal config
COPY ReportPortal.config.json /app/SharedTests
COPY ReportPortal.config.json /app/ApiTests
COPY ReportPortal.config.json /app/AdminTests
COPY ReportPortal.config.json /app/SchedulerTests

WORKDIR /app

# report portal lib
RUN dotnet add SharedTests package ReportPortal.XUnit
RUN dotnet add ApiTests package ReportPortal.XUnit
RUN dotnet add AdminTests package ReportPortal.XUnit
RUN dotnet add SchedulerTests package ReportPortal.XUnit

# start report portal launch
RUN curl --location --request POST $REPORT_SERVER_URL \
    --header "Authorization: Bearer $ReportPortal_Server_Authentication_Uuid" \
    --header 'Content-Type: application/json' \
    --data-raw "{\"name\": \"$ReportPortal_launch_name\",\"startTime\": $(date -u +\"%Y-%m-%dT%H:%M:%SZ\")}" | sed 's/.*"id":"\(.*\)",.*/\1/' > reportid.txt

# run all unittest
RUN export REPORTPORTAL_LAUNCH_ID=$( cat reportid.txt ); \
  dotnet test SharedTests/SharedTests.csproj /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:Exclude=\"[*.Views*]*,[*Test*]*\"; \
  dotnet test ApiTests/ApiTests.csproj /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:Exclude=\"[*.Views*]*,[*Test*]*\"; \
  dotnet test AdminTests/AdminTests.csproj /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:Exclude=\"[*.Views*]*,[*Test*]*\"; \
  dotnet test SchedulerTests/SchedulerTests.csproj /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:Exclude=\"[*.Views*]*,[*Test*]*\";

# close report portal launch
RUN export REPORTPORTAL_LAUNCH_ID=$( cat reportid.txt ); curl --request PUT "$REPORT_SERVER_URL/${REPORTPORTAL_LAUNCH_ID}/finish" \
    --header "Authorization: Bearer $ReportPortal_Server_Authentication_Uuid" \
    --header 'Content-Type: application/json' \
    --data-raw "{\"status\": \"PASSED\", \"endTime\": $(date -u +\"%Y-%m-%dT%H:%M:%SZ\")}"

# sonarqube scan
RUN dotnet build-server shutdown
RUN dotnet sonarscanner begin /k:"price-service" \
    /v:$(cat /app/version.txt) \
    /d:sonar.host.url=https://sonarqube.central.tech \
    /d:sonar.login=$SONAR_LOGIN \
    /d:sonar.cs.opencover.reportsPaths=**/coverage.opencover.xml \
    /d:sonar.coverage.exclusions=**/*Test*.cs,**/*Mock*.cs,**/Debug/**/*,**/Startup.cs,**/Program.cs,MongoMigrations/**/*,**/MessageBus.cs
RUN dotnet build
RUN dotnet sonarscanner end /d:sonar.login=$SONAR_LOGIN

ENTRYPOINT ["/bin/bash"]