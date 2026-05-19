#!/usr/bin/env pwsh
# Seeds the deterministic account used by the vault health analysis demo.

$ErrorActionPreference = "Stop"

$seederProject = Join-Path $PSScriptRoot ".." "util" "SeederUtility"

dotnet run --project $seederProject -- individual `
    --subscription premium `
    --first-name Demo `
    --last-name User `
    --email vaulthealth@bw.test `
    --password asdfasdfasdf `
    --vault `
    --skip-if-exists
