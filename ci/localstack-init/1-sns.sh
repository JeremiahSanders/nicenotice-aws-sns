#!/usr/bin/env bash
set -euo pipefail

echo "[init] Starting SNS initialization..."

AWS_REGION="us-east-1"

create_sns_topic() {
    local topic_name="$1"

    echo "[init] Creating SNS topic: ${topic_name}"

    # awslocal is provided in the LocalStack container and injects endpoint + credentials
    awslocal sns create-topic \
        --region "${AWS_REGION}" \
        --name "${topic_name}" >/dev/null

    echo "[init] Created topic: ${topic_name}"
}

list_sns_topics() {
    echo "[init] Listing all SNS topics..."

    awslocal sns list-topics \
        --region "${AWS_REGION}"
}

# --- Initialize Topics ---
create_sns_topic "primary-message-stream"
create_sns_topic "high-priority-message-stream"
create_sns_topic "error-stream"

# --- List Topics ---
list_sns_topics

echo "[init] SNS initialization complete."
