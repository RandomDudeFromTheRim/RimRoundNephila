#!/bin/sh
# Builds Nephila.dll into 1.6/Assemblies against the game's own assemblies; prints errors/warnings only.
cd "$(dirname "$0")/Nephila"
M='C:\Program Files (x86)\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed'
dotnet build Nephila.csproj -nologo -v:q -p:Deterministic=false "-p:FrameworkPathOverride=$M" "-p:TargetFrameworkDirectory=$M" 2>&1 \
  | grep -E "error|warning CS" | sed -E 's/ \[[^]]*\]$//' | sort -u
ls -la ../../1.6/Assemblies/Nephila.dll
# Vanilla Psycasts Expanded support (needs VPE and the Vanilla Expanded Framework from the Workshop)
cd ../NephilaVPE
dotnet build NephilaVPE.csproj -nologo -v:q -p:Deterministic=false "-p:FrameworkPathOverride=$M" "-p:TargetFrameworkDirectory=$M" 2>&1 | grep -E "error|warning CS" | sed -E 's/ \[[^]]*\]$//' | sort -u
ls -la ../../1.6/Mods/VPE/Assemblies/NephilaVPE.dll
