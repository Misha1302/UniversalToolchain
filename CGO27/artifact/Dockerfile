FROM mcr.microsoft.com/dotnet/sdk:10.0-noble
RUN apt-get update \
    && DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends python3 git ca-certificates \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /artifact
COPY . /artifact
CMD ["bash", "quick-check.sh", "/artifact/artifacts/docker-quick-check"]
