#!/bin/sh

TEST_PROJECT=./src/IxMilia.Config.Test/IxMilia.Config.Test.csproj
dotnet restore $TEST_PROJECT
dotnet test $TEST_PROJECT
