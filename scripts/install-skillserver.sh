#!/usr/bin/env bash
#
# Usage:
#   curl -fsSL https://raw.githubusercontent.com/netclaw-dev/skill-server/dev/scripts/install-skillserver.sh | bash
#   ./install-skillserver.sh
#   ./install-skillserver.sh 0.2.1
#   INSTALL_DIR=/custom/path ./install-skillserver.sh
#
set -euo pipefail

REPO="netclaw-dev/skill-server"
BINARY_NAME="skillserver"
INSTALL_DIR="${INSTALL_DIR:-${HOME}/.local/bin}"
VERSION="${1:-latest}"

detect_platform() {
    local os arch
    os="$(uname -s)"
    arch="$(uname -m)"

    case "$os" in
        Linux)  os="linux" ;;
        Darwin) os="osx" ;;
        *)
            echo "Error: Unsupported OS: $os" >&2
            exit 1
            ;;
    esac

    case "$arch" in
        x86_64|amd64) arch="x64" ;;
        aarch64|arm64) arch="arm64" ;;
        *)
            echo "Error: Unsupported architecture: $arch" >&2
            exit 1
            ;;
    esac

    echo "${os}-${arch}"
}

resolve_version() {
    if [ "$VERSION" = "latest" ]; then
        VERSION=$(curl -sI "https://github.com/${REPO}/releases/latest" \
            | grep -i "^location:" \
            | sed 's|.*/||' \
            | tr -d '\r\n')

        if [ -z "$VERSION" ]; then
            echo "Error: Could not determine latest version" >&2
            exit 1
        fi
    fi
    VERSION="${VERSION#v}"
}

main() {
    local rid archive_name download_url checksum_url tmp_dir

    echo "Installing ${BINARY_NAME}..."

    rid=$(detect_platform)
    resolve_version

    archive_name="${BINARY_NAME}-${VERSION}-${rid}.tar.gz"
    download_url="https://github.com/${REPO}/releases/download/${VERSION}/${archive_name}"
    checksum_url="${download_url}.sha256"

    tmp_dir=$(mktemp -d)
    trap 'rm -rf "$tmp_dir"' EXIT

    echo "  Platform:  ${rid}"
    echo "  Version:   ${VERSION}"
    echo "  Directory: ${INSTALL_DIR}"
    echo ""

    echo "  Downloading ${archive_name}..."
    if ! curl -fsSL -o "${tmp_dir}/${archive_name}" "$download_url"; then
        echo "Error: Failed to download ${archive_name}" >&2
        echo "  URL: ${download_url}" >&2
        echo "  Available platforms: linux-x64, linux-arm64, osx-arm64" >&2
        exit 1
    fi

    echo "  Verifying checksum..."
    if curl -fsSL -o "${tmp_dir}/${archive_name}.sha256" "$checksum_url" 2>/dev/null; then
        cd "$tmp_dir"
        if command -v sha256sum > /dev/null 2>&1; then
            sha256sum -c "${archive_name}.sha256" --quiet
        elif command -v shasum > /dev/null 2>&1; then
            expected=$(awk '{print $1}' "${archive_name}.sha256")
            actual=$(shasum -a 256 "${archive_name}" | awk '{print $1}')
            if [ "$expected" != "$actual" ]; then
                echo "Error: Checksum mismatch" >&2
                exit 1
            fi
        fi
        cd - > /dev/null
    else
        echo "  Warning: Checksum file not available, skipping verification" >&2
    fi

    echo "  Extracting..."
    tar -xzf "${tmp_dir}/${archive_name}" -C "$tmp_dir"

    mkdir -p "${INSTALL_DIR}"
    cp "${tmp_dir}/${BINARY_NAME}" "${INSTALL_DIR}/${BINARY_NAME}"
    chmod +x "${INSTALL_DIR}/${BINARY_NAME}"

    echo ""
    echo "  Installed ${BINARY_NAME} ${VERSION} to ${INSTALL_DIR}/${BINARY_NAME}"

    case ":${PATH}:" in
        *":${INSTALL_DIR}:"*) ;;
        *)
            echo ""
            echo "  Warning: ${INSTALL_DIR} is not in your PATH."
            echo "  Add it by running:"
            echo ""
            if [ -n "${ZSH_VERSION:-}" ] || [ -f "${HOME}/.zshrc" ]; then
                echo "    echo 'export PATH=\"${INSTALL_DIR}:\$PATH\"' >> ~/.zshrc && source ~/.zshrc"
            else
                echo "    echo 'export PATH=\"${INSTALL_DIR}:\$PATH\"' >> ~/.bashrc && source ~/.bashrc"
            fi
            ;;
    esac

    echo ""
    echo "  Run '${BINARY_NAME} --version' to verify."
}

main
