#!/bin/sh -e

_SCRIPT_DIR="$( cd -P -- "$(dirname -- "$(command -v -- "$0")")" && pwd -P )"
PROJECT=$_SCRIPT_DIR/IxMilia.Config/IxMilia.Config.csproj
dotnet restore $PROJECT
dotnet pack --configuration Release $PROJECT
