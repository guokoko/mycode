ARG DOTNET_TAG=3.1
FROM mcr.microsoft.com/dotnet/core/sdk:$DOTNET_TAG AS build
RUN apt-get update \
  && apt-get install openjdk-11-jdk -y
RUN dotnet tool install --global dotnet-sonarscanner

# install dotnet-runtime-2.1 for error CGR1001: CodeGeneration.Roslyn.Tool (dotnet-codegen) is not available
RUN wget -qO- https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.asc.gpg && \
    mv microsoft.asc.gpg /etc/apt/trusted.gpg.d/ && \
    wget -q https://packages.microsoft.com/config/debian/10/prod.list && \
    mv prod.list /etc/apt/sources.list.d/microsoft-prod.list && \
    chown root:root /etc/apt/trusted.gpg.d/microsoft.asc.gpg && \
    chown root:root /etc/apt/sources.list.d/microsoft-prod.list && apt-get update
RUN apt-get install -y dotnet-runtime-2.1

ENV PATH="$PATH:/root/.dotnet/tools"