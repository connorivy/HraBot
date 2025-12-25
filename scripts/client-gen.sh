#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
api_project="$repo_root/src/HraBot.Api/HraBot.Api.csproj"
api_openapi_json="$repo_root/src/HraBot.Api/obj/HraBot.Api.json"
web_output_dir="$repo_root/src/hrabot-web/src/hraBotApiClient"

dotnet build "$api_project"

if [[ ! -f "$api_openapi_json" ]]; then
  echo "Expected OpenAPI document not found: $api_openapi_json" >&2
  exit 1
fi

if [[ ! -d "$web_output_dir" ]]; then
  echo "Expected web output directory not found: $web_output_dir" >&2
  exit 1
fi

find "$web_output_dir" -mindepth 1 ! -name '.gitignore' -exec rm -rf {} +

dnx Microsoft.OpenApi.Kiota@1.29.0 \
  --allow-roll-forward \
  --yes \
  -- generate \
  -l typescript \
  -c HraBotApiClient \
  -n HraBot.ApiClient \
  -d "$api_openapi_json" \
  -o "$web_output_dir"
