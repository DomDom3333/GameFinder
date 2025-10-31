# Download Guide

This guide helps you choose the correct build for your system.

## Client Downloads

### Windows
- **ArcadeMatch.Client-Windows-x64.zip** - For 64-bit Windows systems

### Linux
Choose based on your distribution and architecture:

#### x64 / AMD64 Systems (Standard Desktop/Laptop)
- **ArcadeMatch.Client-Linux-x64.zip** - For most Linux distributions including:
  - Debian, Ubuntu, Linux Mint, Pop!_OS, Zorin OS, elementary OS
  - Arch Linux, Manjaro, EndeavourOS, Garuda Linux
  - Fedora, RHEL, CentOS, Rocky Linux, AlmaLinux
  - openSUSE, SUSE Linux Enterprise
  - Any other glibc-based distribution

- **ArcadeMatch.Client-Linux-Alpine-x64.zip** - For Alpine Linux and other musl-based distributions

#### ARM Systems
- **ArcadeMatch.Client-Linux-ARM64.zip** - For 64-bit ARM devices (Raspberry Pi 4/5, Pine64, etc.)
- **ArcadeMatch.Client-Linux-ARM.zip** - For 32-bit ARM devices (older Raspberry Pi models)
- **ArcadeMatch.Client-Linux-Alpine-ARM64.zip** - For Alpine Linux on ARM64

### macOS
- **ArcadeMatch.Client-macOS-Intel.zip** - For Intel-based Macs
- **ArcadeMatch.Client-macOS-AppleSilicon.zip** - For M1/M2/M3/M4 Macs (Apple Silicon)

## Server Downloads

Server builds follow the same naming pattern and compatibility:

### Windows
- **ArcadeMatch.Server-Windows-x64.zip**

### Linux
- **ArcadeMatch.Server-Linux-x64.zip** - Ubuntu, Debian, Arch, Fedora, etc.
- **ArcadeMatch.Server-Linux-Alpine-x64.zip** - Alpine Linux
- **ArcadeMatch.Server-Linux-ARM64.zip** - ARM64 devices
- **ArcadeMatch.Server-Linux-ARM.zip** - 32-bit ARM devices
- **ArcadeMatch.Server-Linux-Alpine-ARM64.zip** - Alpine Linux on ARM64

### macOS
- **ArcadeMatch.Server-macOS-Intel.zip** - Intel Macs
- **ArcadeMatch.Server-macOS-AppleSilicon.zip** - Apple Silicon Macs

## How to Check Your System

### Linux
```bash
# Check architecture
uname -m
# x86_64 = use x64 builds
# aarch64 = use ARM64 builds
# armv7l = use ARM builds

# Check if using musl (Alpine)
ldd --version 2>&1 | grep -q musl && echo "Use Alpine builds" || echo "Use standard Linux builds"
```

### macOS
```bash
# Check CPU type
uname -m
# x86_64 = Intel Mac
# arm64 = Apple Silicon (M1/M2/M3/M4)
```

### Windows
- Almost all modern Windows systems are x64
- Check System Settings → About to confirm

## Installation Notes

All builds are self-contained, meaning they include all required dependencies and don't require installing .NET separately.

### Linux Permissions
After extracting, you may need to make the executable file executable:
```bash
chmod +x ArcadeMatch.Client  # or ArcadeMatch.Server
```

### macOS Security
On macOS, you may need to allow the app in System Preferences → Security & Privacy when first launching.

