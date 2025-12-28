#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/../src/HraBot.Core"

rm -rf ./Generated/*
# find "./Generated" -mindepth 1 ! -name '.gitignore' -exec rm -rf {} +

dnx dotnet-ef@10.0.1 dbcontext optimize \
  --output-dir Generated \
  --precompile-queries \
  --nativeaot \
  --startup-project ../HraBot.Api
