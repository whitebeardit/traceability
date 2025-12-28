#!/bin/bash
# Script para atualizar versão no .csproj

set -e

VERSION="$1"
CSPROJ_FILE="src/Traceability/Traceability.csproj"

if [ -z "$VERSION" ]; then
    echo "Usage: $0 <version>"
    exit 1
fi

# Atualizar versão no .csproj
if [[ "$OSTYPE" == "darwin"* ]]; then
    # macOS
    sed -i '' "s/<Version>.*<\/Version>/<Version>${VERSION}<\/Version>/" "$CSPROJ_FILE"
else
    # Linux
    sed -i "s/<Version>.*<\/Version>/<Version>${VERSION}<\/Version>/" "$CSPROJ_FILE"
fi

echo "Updated version to ${VERSION} in ${CSPROJ_FILE}"







