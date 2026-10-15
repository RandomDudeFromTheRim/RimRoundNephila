#!/bin/sh
# Builds Nephila.dll into 1.6/Assemblies; prints errors/warnings only.
cd "$(dirname "$0")/Nephila"
# RimWorld 1.7 dropped RimWorldWin64_Data/Managed: the game and Unity assemblies come from
# RimRound's Source/Dev (1.6), the framework from the .NET Framework 4.8 reference assemblies.
G='C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\RimRound\Source\Dev\RimWorldWin64_Data\Managed'
M="$HOME/.nuget/packages/microsoft.netframework.referenceassemblies.net48/1.0.3/build/.NETFramework/v4.8"
dotnet build Nephila.csproj -nologo -v:q -p:Deterministic=false "-p:FrameworkPathOverride=$M" "-p:TargetFrameworkDirectory=$M" "-p:Managed=$G" "-p:Framework=$M" "-p:FrameworkFacades=$M/Facades" 2>&1 \
  | grep -E "error|warning CS" | sed -E 's/ \[[^]]*\]$//' | sort -u
ls -la ../../1.6/Assemblies/Nephila.dll
# Vanilla Psycasts Expanded support (needs VPE and the Vanilla Expanded Framework from the Workshop)
cd ../NephilaVPE
dotnet build NephilaVPE.csproj -nologo -v:q -p:Deterministic=false "-p:FrameworkPathOverride=$M" "-p:TargetFrameworkDirectory=$M" "-p:Managed=$G" "-p:Framework=$M" "-p:FrameworkFacades=$M/Facades" 2>&1 | grep -E "error|warning CS" | sed -E 's/ \[[^]]*\]$//' | sort -u
ls -la ../../1.6/Mods/VPE/Assemblies/NephilaVPE.dll
