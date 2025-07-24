# Define variables
ANDROID_HOME="$HOME/Android/sdk"
ANDROID_SDK_TOOLS_URL="https://dl.google.com/android/repository/commandlinetools-linux-13114758_latest.zip" # This URL might change, verify periodically.
SDK_TOOLS_ZIP="commandlinetools-linux.zip"
JAVA_VERSION="openjdk-17-jdk" # .NET 8 (LTS) requires JDK 17 for Android development.

echo "Starting Android development tools installation for .NET on Ubuntu 22.04..."

# 1. Update package lists
echo "1. Updating package lists..."
sudo apt update -y

# 2. Install essential packages
echo "2. Installing essential packages (unzip, default-jre for initial setup)..."
sudo apt install -y unzip ${JAVA_VERSION} curl

## 3. Configure Java environment (if not already set by default-jre)
#echo "3. Setting JAVA_HOME environment variable..."
#JAVA_PATH=$(dirname $(dirname $(readlink -f $(which java))))
#echo "Found Java path: ${JAVA_PATH}"
#echo "export JAVA_HOME=${JAVA_PATH}" >> "$HOME/.profile"
#echo "export PATH=$PATH:$JAVA_HOME/bin" >> "$HOME/.profile"
## Source profile for immediate effect in this script's context
#export JAVA_HOME=${JAVA_PATH}
#export PATH=$PATH:$JAVA_HOME/bin

# 4. Create Android SDK directory
echo "4. Creating Android SDK directory at ${ANDROID_HOME}..."
mkdir -p "${ANDROID_HOME}"

# 5. Download Android Command-line Tools
echo "5. Downloading Android Command-line Tools from ${ANDROID_SDK_TOOLS_URL}..."
curl -L -o "/tmp/${SDK_TOOLS_ZIP}" "${ANDROID_SDK_TOOLS_URL}"

# 6. Unzip Android Command-line Tools
echo "6. Unzipping Android Command-line Tools to ${ANDROID_HOME}/cmdline-tools/latest..."
# Google's recommendation is to extract to ANDROID_HOME/cmdline-tools/latest
# Create the parent directory first
mkdir -p "${ANDROID_HOME}/cmdline-tools"
unzip -q "/tmp/${SDK_TOOLS_ZIP}" -d "${ANDROID_HOME}"

# 7. Set ANDROID_HOME and PATH environment variables
echo "7. Setting ANDROID_HOME and adding SDK tools to PATH..."
#echo "export ANDROID_HOME=${ANDROID_HOME}" >> "$HOME/.profile"
#echo "export PATH=\$PATH:${ANDROID_HOME}/cmdline-tools/bin" >> "$HOME/.profile"
#echo "export PATH=\$PATH:${ANDROID_HOME}/platform-tools" >> "$HOME/.profile" # Platform tools are installed later by sdkmanager

# Source profile again for immediate effect in this script's context
export ANDROID_HOME="${ANDROID_HOME}"
export PATH="$PATH:${ANDROID_HOME}/cmdline-tools/bin"
export PATH="$PATH:${ANDROID_HOME}/platform-tools"

# 8. Accept Android SDK licenses (REQUIRED for sdkmanager to work)
echo "8. Accepting Android SDK licenses..."
yes | "${ANDROID_HOME}/cmdline-tools/cmdline-tools/bin/sdkmanager" --licenses

# 9. Install necessary Android SDK components using sdkmanager
echo "9. Installing Android SDK Platforms, Platform Tools, and Build Tools..."
# Adjust these versions as needed. For .NET MAUI, target Android API 33 or 34.
# platform-tools is essential for adb.
# build-tools are needed for compiling.
# platforms;android-XX is for the target Android version.
"${ANDROID_HOME}/cmdline-tools/latest/bin/sdkmanager" \
    "platform-tools" \
    "build-tools;34.0.0" \
    "platforms;android-34" \
    "emulator" # Install emulator if you plan to use Android emulators

# Verify installation
echo "10. Verifying sdkmanager and avdmanager availability..."
if command -v sdkmanager &>/dev/null; then
    echo "sdkmanager is available at $(which sdkmanager)"
else
    echo "Error: sdkmanager not found!"
    exit 1
fi

if command -v avdmanager &>/dev/null; then
    echo "avdmanager is available at $(which avdmanager)"
else
    echo "Error: avdmanager not found!"
    exit 1
fi

echo "Android development tools installation complete."
echo "Please log out and log back in, or run 'source ~/.profile' to apply environment variables in new shells."
echo "You can now use 'sdkmanager' and 'avdmanager'."
echo "For .NET Android development, you will also need to install the .NET SDK."
echo "Instructions for .NET SDK: https://learn.microsoft.com/en-us/dotnet/core/install/linux-ubuntu-2204"
