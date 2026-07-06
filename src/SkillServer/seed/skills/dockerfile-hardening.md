---
name: dockerfile-hardening
description: Review and harden Dockerfiles for security, image size, build reproducibility, and runtime safety
license: Apache-2.0
metadata:
  category: security
  version: "1.0"
  tags: [docker, container-security, ci, devops]
---

# Dockerfile Hardening

Review Dockerfiles for security vulnerabilities, image bloat, and operational risks. Apply defense-in-depth principles across build stages, runtime configuration, and supply chain hygiene.

## Build-Time Hardening

### Multi-Stage Builds

Use multi-stage builds to separate build toolchains from runtime images. The final image should contain only what the application needs to run — no compilers, no package managers, no source code.

For .NET applications, use the SDK image for build stages and `aspnet` (with `cherry-pick` or `alpine` variants) for runtime:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY *.sln .
COPY **/*.csproj ./
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "myapp.dll"]
```

This pattern reduces the final image from ~1.8GB (SDK) to ~200MB (runtime). Every layer that leaks build artifacts into the final image is a wasted attack surface.

### Layer Pinning

Pin base image digests instead of tags. Tags are mutable — `latest`, `8.0`, even `8.0-jammy` can be repushed. Digest pinning ensures build reproducibility:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0@sha256:a3f4b2c1d5e6f7...
```

If your CI doesn't pin digests, add a step that fetches and updates them before build. Tools like `dive` or `trivy` can help verify you're not pulling a mutated tag.

### Build Secrets

Never bake secrets into image layers. Use `--mount=type=secret` in BuildKit to pass secrets during build without leaving them in the image history:

```dockerfile
# syntax=docker/dockerfile:1.4
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
RUN --mount=type=secret,id=nuget_config \
    cp /run/secrets/nuget_config /tmp/nuget.config && \
    dotnet restore --configfile /tmp/nuget.config
```

Do not use `ARG` for secrets — they persist in the image metadata and are visible via `docker history`. Do not `COPY` files containing tokens from your repo.

### Dependency Scanning

Run a vulnerability scan on the final image as a CI gate. Use Trivy, Grype, or Snyk with a severity threshold that blocks promotion:

```bash
trivy image --severity CRITICAL,HIGH --exit-code 1 myapp:latest
```

Scan the build image too — a vulnerable SDK image can compromise the build even if the runtime is clean.

## Runtime Configuration

### Non-Root User

Containers must run as non-root. Create a dedicated user with minimal permissions:

```dockerfile
RUN groupadd -r appuser && useradd -r -g appuser -d /home/appuser -s /sbin/nologin appuser
USER appuser
```

The user should not have a login shell (`/sbin/nologin`) because if someone gets a shell into the container, they shouldn't have a default environment to play with.

### File Permissions

Set restrictive permissions on application files. The user should only read what it needs:

```dockerfile
RUN chown -R appuser:appuser /app && \
    chmod -R 444 /app && \
    chmod 555 /app/myapp.dll
```

For .NET apps, the runtime only needs read and execute on the binary. If your app writes to disk (logs, temp files), create a specific directory and grant write access only there:

```dockerfile
RUN mkdir -p /app/logs && chown appuser:appuser /app/logs
```

### Drop Capabilities

Even non-root containers inherit Linux capabilities. Drop all and add back only what you need:

```dockerfile
# In your deployment manifest, not the Dockerfile itself:
# securityContext:
#   capabilities:
#     drop: ["ALL"]
#     add: ["NET_BIND_SERVICE"]  # only if binding to port < 1024
```

This is usually configured in Kubernetes manifests or Docker Compose files, but flag it during Dockerfile review if the app requires privileged operations.

### Health Checks

Every production image should have a health check. Without one, orchestration systems can't distinguish between a live but sick container and a dead one:

```dockerfile
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1
```

For .NET apps, use the built-in health endpoint rather than a generic port check. The health endpoint validates dependencies (database, cache), not just whether the process is running.

## Image Optimization

### Slim Base Images

Choose the smallest safe base image:

- `cherry-pick` — Microsoft's minimal runtime image, smaller than full OS but larger than Alpine
- `alpine` — smallest, but may have compatibility issues with some native libraries
- Full OS images (Debian, Ubuntu) — avoid unless you need packages not available elsewhere

For .NET, `cherry-pick` is the recommended default. Alpine works but has had glibc compatibility issues with certain NuGet packages.

### Layer Ordering

Order instructions by change frequency. Infrequently changing layers (base image, SDK install) go first. Frequently changing layers (source code) go last. Docker caches layers that haven't changed, so poor ordering wastes build time:

```dockerfile
# Bad: source code copied before restore, busts cache on every edit
COPY . .
RUN dotnet restore

# Good: project files copied and restored before source, reuses cache
COPY **/*.csproj ./
RUN dotnet restore
COPY . .
```

### Clean Up in the Same Layer

Package manager caches and build artifacts bloat images. Clean up in the same RUN instruction that creates the junk — a new layer on top of a cleaned layer is pointless since the fat layer is still in the history:

```dockerfile
# Bad: cache cleaned in separate layer, fat layer still in history
RUN apt-get update && apt-get install -y curl
RUN apt-get clean && rm -rf /var/lib/apt/lists/*

# Good: install and clean in one layer
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
```

### Minimize Install Commands

Each RUN instruction creates a layer. Combine related commands, but don't make one instruction so complex that it can't be read or cached:

```dockerfile
RUN apt-get update && apt-get install -y --no-install-recommends \
    curl \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/*
```

Avoid installing `wget`, `git`, `make`, `gcc` in runtime images. If you need them for build, use a build stage.

## Supply Chain

### SBOM Generation

Generate a Software Bill of Materials (SBOM) during build. This is becoming a compliance requirement (Executive Order 14028 in the US, similar requirements in EU):

```dockerfile
# Add to CI pipeline, not the image:
# syft myapp:latest > sbom.json
```

Syft or CycloneDX generators produce structured SBOMs. Include this in your CI pipeline as an artifact, not as part of the image.

### Image Signing

Sign images with Cosign or Notary. Signed images prove provenance — that the image was built by your pipeline and hasn't been tampered with:

```bash
cosign sign --key cosign.key myregistry.com/myapp:latest
```

In deployment manifests, verify signatures before running:

```yaml
cosign verify --key cosign.pub myregistry.com/myapp:latest
```

### Reproducible Builds

Set build timestamps to a fixed value so identical builds produce identical images:

```dockerfile
ARG BUILDKIT_INLINE_CACHE=1
# In CI:
# docker build --build-arg SOURCE_DATE_EPOCH=$(git log -1 --format=%ct) .
```

Reproducible builds make it possible to detect tampering and verify that a given commit produces a given image.

## Common Anti-Patterns

### `COPY . .` in Build Stages

If your `.dockerignore` is misconfigured, this copies your `.git` directory, local secrets, IDE configs, and test results into the image. Always check `.dockerignore` has at least:

```
.git
.gitignore
*.md
*.mdoc
**/.vs/
**/bin/
**/obj/
.dockerignore
Dockerfile
```

### Running `apt-get update` Without `--no-install-recommends`

The `recommends` pull in packages you don't need. A single `curl` install with recommends adds ~30MB of unrelated packages.

### Using `docker:dind` for Build Agents

Running Docker-in-Docker for CI builds creates privileged containers. Use BuildKit with `docker buildx` and a socket mount instead, or use Kaniko for container-native builds.

### Hardcoding Registry URLs

Dockerfiles should not contain registry authentication or registry-specific URLs. Those belong in CI configuration and deployment manifests. The Dockerfile should reference images by name and let the orchestrator resolve them.

### Exposing Unnecessary Ports

Only expose the port the application listens on. A common mistake is exposing both HTTP (80) and HTTPS (443) when the container only serves on one. Exposed ports don't publish them, but they document intent — and incorrect documentation leads to incorrect deployment.

## Review Checklist

When reviewing a Dockerfile, check:

1. Multi-stage build used to separate build from runtime
2. Base images pinned by digest or at minimum by specific tag
3. Non-root user with no login shell
4. File permissions restrict read/write to minimum needed
5. Health check configured for orchestration integration
6. No secrets in ARG, ENV, or COPY
7. Package manager caches cleaned in the same layer
8. `.dockerignore` excludes source control, IDE, and build artifacts
9. No unnecessary packages in the final image
10. Image includes minimal labels (maintainer deprecated, use org.opencontainers annotations)
11. ENTRYPOINT and CMD use JSON array form (avoids shell wrapper)
12. No `ONBUILD` instructions (they create invisible build-time behavior)