#!/bin/bash
set -e

echo "🔨 Building .deb package with embedded gemma2-2b model..."

DEB_ROOT="/home/mezouarsohaib/tabib-ai/deb-package"
MODEL_SOURCE="/home/mezouarsohaib/tabib-ai/release-files/gemma2-2b.gguf"
MODEL_DEST="$DEB_ROOT/opt/tabib-ai/Models/gemma2-2b.gguf"
OUTPUT_DEB="/home/mezouarsohaib/tabib-ai/tabib-ai_1.1.0-1_amd64.deb"

# Create Models directory
mkdir -p "$DEB_ROOT/opt/tabib-ai/Models"

# Copy model if not already there
if [ ! -f "$MODEL_DEST" ]; then
    echo "📦 Copying gemma2-2b.gguf (1.6 GB) into deb package..."
    cp "$MODEL_SOURCE" "$MODEL_DEST"
    echo "✅ Model copied"
else
    echo "✅ Model already exists in deb package"
fi

# Verify model is in place
ls -lh "$MODEL_DEST"

# Build the .deb package
echo "📦 Building .deb package..."
cd /home/mezouarsohaib/tabib-ai
dpkg-deb --build "$DEB_ROOT" "$OUTPUT_DEB"

echo "✅ .deb package built: $OUTPUT_DEB"
ls -lh "$OUTPUT_DEB"

# Verify model is inside the deb
echo "🔍 Verifying model inside .deb..."
dpkg-deb -c "$OUTPUT_DEB" | grep gemma2-2b.gguf
