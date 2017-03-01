#!/bin/sh

PROJECT=./IxMilia.Config/IxMilia.Config.csproj
dotnet restore $PROJECT
dotnet pack --include-symbols --include-source --configuration Release $PROJECT
