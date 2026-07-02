#!/bin/bash
# Packages Translator.Desktop as a standalone macOS .app bundle in dist/.
set -euo pipefail

cd "$(dirname "$0")/.."
RID="${1:-osx-arm64}"
VERSION="${VERSION:-1.0.0}"
APP="dist/Translate.app"

dotnet publish src/Translator.Desktop -c Release -r "$RID" --self-contained \
    -p:PublishSingleFile=false -p:Version="$VERSION" -o "dist/publish-$RID"

rm -rf "$APP"
mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"
cp -R "dist/publish-$RID/." "$APP/Contents/MacOS/"
cp src/Translator.Desktop/Assets/AppIcon.icns "$APP/Contents/Resources/"

cat > "$APP/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleName</key><string>Translate</string>
    <key>CFBundleDisplayName</key><string>Translate</string>
    <key>CFBundleIdentifier</key><string>dev.laszloprekop.translate</string>
    <key>CFBundleVersion</key><string>${VERSION}</string>
    <key>CFBundleShortVersionString</key><string>${VERSION}</string>
    <key>CFBundlePackageType</key><string>APPL</string>
    <key>CFBundleExecutable</key><string>Translator.Desktop</string>
    <key>CFBundleIconFile</key><string>AppIcon</string>
    <key>NSHighResolutionCapable</key><true/>
    <key>LSMinimumSystemVersion</key><string>11.0</string>
</dict>
</plist>
PLIST

codesign --force --deep --sign - "$APP"
echo "Packaged $APP"
