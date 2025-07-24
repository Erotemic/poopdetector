#!/bin/bash
set -euo pipefail

#ANDROID_STUDIO_URL="https://redirector.gvt1.com/edgedl/android/studio/ide-zips/2024.1.1.21/android-studio-2024.1.1.21-linux.tar.gz"
ANDROID_STUDIO_URL="https://redirector.gvt1.com/edgedl/android/studio/ide-zips/2025.1.1.14/android-studio-2025.1.1.14-linux.tar.gz"

INSTALL_DIR="$HOME/.local/opt/android-studio"
DESKTOP_ENTRY_DIR="$HOME/.local/share/applications"
DESKTOP_ENTRY_PATH="$DESKTOP_ENTRY_DIR/android-studio.desktop"
BIN_SYMLINK="$HOME/.local/bin/android-studio"

# Ensure required tools
command -v curl >/dev/null || { echo "❌ Please install curl."; exit 1; }
command -v tar  >/dev/null || { echo "❌ Please install tar."; exit 1; }

# Create necessary directories
mkdir -p "$INSTALL_DIR"
mkdir -p "$DESKTOP_ENTRY_DIR"
mkdir -p "$(dirname "$BIN_SYMLINK")"

# Use a temporary file for the tarball
TARBALL="$(mktemp --suffix=.tar.gz)"
echo "⬇️ Downloading Android Studio..."
curl -L -o "$TARBALL" "$ANDROID_STUDIO_URL"

# Extract directly into INSTALL_DIR
echo "📦 Extracting to $INSTALL_DIR..."
rm -rf "$INSTALL_DIR"
mkdir -p "$INSTALL_DIR"
tar -xzf "$TARBALL" --strip-components=1 -C "$INSTALL_DIR"
rm -f "$TARBALL"

# Create symlink for CLI use
ln -sf "$INSTALL_DIR/bin/studio.sh" "$BIN_SYMLINK"

# Create a desktop launcher
echo "🖥️ Creating desktop entry at $DESKTOP_ENTRY_PATH..."
cat > "$DESKTOP_ENTRY_PATH" <<EOF
[Desktop Entry]
Version=1.0
Type=Application
Name=Android Studio
Exec=$INSTALL_DIR/bin/studio.sh
Icon=$INSTALL_DIR/bin/studio.png
Categories=Development;IDE;
Terminal=false
EOF

chmod +x "$DESKTOP_ENTRY_PATH"

echo "✅ Android Studio installed in $INSTALL_DIR"
echo "📎 Desktop entry available at: $DESKTOP_ENTRY_PATH"
echo "🟢 Launch from menu or run: android-studio"
