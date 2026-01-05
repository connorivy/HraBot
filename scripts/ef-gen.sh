#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/../src/HraBot.Core"

rm -rf ./Generated/EF
# find "./Generated/EF" -mindepth 1 ! -name '.gitignore' -exec rm -rf {} +

dotnet build /p:GENERATING_EF=true

# dnx dotnet-ef@10.0.1 dbcontext optimize \
#   --output-dir Generated \
#   --precompile-queries \
#   --nativeaot \
#   --startup-project ../HraBot.Api

dnx dotnet-ef@10.0.1 dbcontext optimize \
  --output-dir Generated/EF \
  --startup-project ../HraBot.Api \
  --no-build
