#!/bin/bash
systemctl stop paga-api.service || true

# Clean deployment directory for fresh install (preserve .env with secrets)
if [ -d /opt/paga/api ]; then
  find /opt/paga/api -mindepth 1 ! -name '.env' -delete 2>/dev/null || true
fi