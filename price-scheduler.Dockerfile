ARG DOTNET_TAG=3.1-alpine3.12

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true
ENV NUGET_XMLDOC_MODE=skip

# install dotnet-runtime-2.1 for error CGR1001: CodeGeneration.Roslyn.Tool (dotnet-codegen) is not available
RUN wget -qO- https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.asc.gpg && \
    mv microsoft.asc.gpg /etc/apt/trusted.gpg.d/ && \
    wget -q https://packages.microsoft.com/config/debian/10/prod.list && \
    mv prod.list /etc/apt/sources.list.d/microsoft-prod.list && \
    chown root:root /etc/apt/trusted.gpg.d/microsoft.asc.gpg && \
    chown root:root /etc/apt/sources.list.d/microsoft-prod.list && apt-get update
RUN apt-get install -y dotnet-runtime-2.1

#RUN wget -q -O /etc/apk/keys/sgerrand.rsa.pub https://alpine-pkgs.sgerrand.com/sgerrand.rsa.pub
#RUN wget https://github.com/sgerrand/alpine-pkg-glibc/releases/download/2.32-r0/glibc-2.32-r0.apk
#RUN apk add glibc-2.32-r0.apk

WORKDIR /build

# copy csproj and restore as distinct layers
COPY PriceScheduler/PriceScheduler.csproj PriceScheduler/
COPY Shared/Shared.csproj Shared/


# copy and publish app and libraries
COPY PriceScheduler PriceScheduler/
COPY Shared Shared/
WORKDIR /build/PriceScheduler
RUN dotnet restore -r linux-musl-x64
RUN dotnet list PriceScheduler.csproj package
RUN dotnet publish -c Release -o /app \
  -r linux-musl-x64 \
  --self-contained true \
  --no-restore

FROM mcr.microsoft.com/dotnet/core/runtime-deps:$DOTNET_TAG AS final

# install the agent
RUN  mkdir /usr/local/newrelic-dotnet-agent \
&& cd /usr/local \
&& export NEW_RELIC_DOWNLOAD_URI=https://download.newrelic.com/$(wget -qO - "https://nr-downloads-main.s3.amazonaws.com/?delimiter=/&prefix=dot_net_agent/latest_release/newrelic-dotnet-agent" | grep -E -o 'dot_net_agent/latest_release/newrelic-dotnet-agent_[[:digit:]]{1,3}(\.[[:digit:]]{1,3}){2}_amd64\.tar\.gz') \
&& echo "Downloading: $NEW_RELIC_DOWNLOAD_URI into $(pwd)" \
&& wget -O - "$NEW_RELIC_DOWNLOAD_URI" | gzip -dc | tar xf -

# Enable the agent
ENV CORECLR_ENABLE_PROFILING=1 \
CORECLR_PROFILER={36032161-FFC0-4B61-B559-F6C5D41BAE5A} \
CORECLR_NEWRELIC_HOME=/usr/local/newrelic-dotnet-agent \
CORECLR_PROFILER_PATH=/usr/local/newrelic-dotnet-agent/libNewRelicProfiler.so

WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["./PriceScheduler"]
