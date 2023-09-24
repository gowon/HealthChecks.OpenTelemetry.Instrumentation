#!/usr/bin/env pwsh
$ErrorActionPreference = "Stop";
dotnet run --project build/build.csproj -c Release -- $args