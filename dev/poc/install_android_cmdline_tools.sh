#!/bin/bash
set -euo pipefail

# Config
#SDK_URL="https://dl.google.com/android/repository/commandlinetools-linux-11076708_latest.zip"
SDK_URL="https://dl.google.com/android/repository/commandlinetools-linux-13114758_latest.zip"
SDK_ROOT="$HOME/.local/opt/android-sdk"
CMDLINE_DIR="$SDK_ROOT/cmdline-tools"
LATEST_DIR="$CMDLINE_DIR/latest"
BIN_DIR="$LATEST_DIR/bin"

# Ensure required tools
command -v curl >/dev/null || { echo "❌ Please install curl."; exit 1; }
command -v unzip >/dev/null || { echo "❌ Please install unzip."; exit 1; }

# Create directories
mkdir -p "$CMDLINE_DIR"
TMP_ZIP="$(mktemp --suffix=.zip)"

# Download SDK command line tools
echo "⬇️ Downloading Android SDK command line tools..."
curl -L -o "$TMP_ZIP" "$SDK_URL"

# Extract to 'latest'
rm -rf "$LATEST_DIR"
mkdir -p "$LATEST_DIR"
unzip -q "$TMP_ZIP" -d "$LATEST_DIR"
rm "$TMP_ZIP"

# Export PATH for current session
export PATH="$BIN_DIR:$PATH"

# Accept licenses
echo "✅ Accepting licenses..."
yes | sdkmanager --licenses >/dev/null

# Optional: install basic tools
echo "📦 Installing platform-tools, emulator, build-tools..."
sdkmanager \
  "platform-tools" \
  "build-tools;34.0.0" \
  "platforms;android-34" \
  "emulator" \
  "system-images;android-34;google_apis;x86_64"

echo "✅ Android SDK CLI installed at: $SDK_ROOT"
echo "➕ Add this to your shell config:"
echo "export PATH=\"$BIN_DIR:\$PATH\""

