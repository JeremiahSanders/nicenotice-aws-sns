#!/usr/bin/env bash
set -euo pipefail

echo "[init] Starting SQS and Subscription initialization..."

AWS_REGION="us-east-1"
ACCOUNT_ID="000000000000"

create_sqs_queue_and_subscribe() {
    local queue_name="$1"
    local topic_name="$2"

    echo "[init] Creating SQS queue: ${queue_name}"
    awslocal sqs create-queue \
        --region "${AWS_REGION}" \
        --queue-name "${queue_name}" >/dev/null

    local topic_arn="arn:aws:sns:${AWS_REGION}:${ACCOUNT_ID}:${topic_name}"
    local queue_arn="arn:aws:sqs:${AWS_REGION}:${ACCOUNT_ID}:${queue_name}"

    echo "[init] Subscribing ${queue_name} to ${topic_name}..."
    awslocal sns subscribe \
        --region "${AWS_REGION}" \
        --topic-arn "${topic_arn}" \
        --protocol sqs \
        --notification-endpoint "${queue_arn}" >/dev/null

    echo "[init] Created and subscribed: ${queue_name}"
}

list_sqs_queues() {
    echo "[init] Listing all SQS queues..."
    awslocal sqs list-queues \
        --region "${AWS_REGION}"
}

# --- Initialize Queues and Subscriptions ---
# Maps queues to the topics created in sns.sh
create_sqs_queue_and_subscribe "primary-queue" "primary-message-stream"
create_sqs_queue_and_subscribe "high-priority-queue" "high-priority-message-stream"
create_sqs_queue_and_subscribe "error-queue" "error-stream"

# --- List Queues ---
list_sqs_queues

echo "[init] SQS initialization complete."
