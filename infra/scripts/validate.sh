#!/bin/bash
# Validate that the PAGA API is healthy after deployment.
# Retries with backoff up to 30 seconds to allow Kestrel startup time.

MAX_WAIT=30
WAITED=0
INTERVALS=(1 2 4 8 15)

for INTERVAL in "${INTERVALS[@]}"; do
  if curl -sf http://localhost:5000/health > /dev/null 2>&1; then
    echo "Health check passed."
    exit 0
  fi

  WAITED=$((WAITED + INTERVAL))
  if [ "$WAITED" -gt "$MAX_WAIT" ]; then
    break
  fi

  echo "Health check failed, retrying in ${INTERVAL}s..."
  sleep "$INTERVAL"
done

# Final attempt after all retries
if curl -sf http://localhost:5000/health > /dev/null 2>&1; then
  echo "Health check passed."
  exit 0
fi

echo "Health check failed after ${WAITED}s. Kestrel may not be running."
exit 1
