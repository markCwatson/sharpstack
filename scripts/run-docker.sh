#!/bin/bash

set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

container_name="sharpstack"
host_port="${HOST_PORT:-8080}"

docker build --tag sharpstack .

docker rm --force "$container_name" >/dev/null 2>&1 || true

docker run --detach --rm \
  --name "$container_name" \
  --publish "127.0.0.1:${host_port}:8080" \
  --device /dev/net/tun \
  --cap-add NET_ADMIN \
  sharpstack

echo "sharpstack is running at http://127.0.0.1:${host_port}/"
echo "Stop it with: docker stop ${container_name}"