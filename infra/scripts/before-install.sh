#!/bin/bash
set -e

# Clean destination directory before CodeDeploy copies new files
# Preserve .env (contains runtime secrets configured outside of deploys)
if [ -d /opt/paga/api ]; then
  find /opt/paga/api -mindepth 1 -not -name '.env' -delete 2>/dev/null || true
fi