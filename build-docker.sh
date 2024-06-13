#!/usr/bin/env bash
dotnet publish src/DrawTogether/DrawTogether.csproj --os linux --arch x64 -c Release -p:PublishProfile=DefaultContainer