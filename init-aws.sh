#!/bin/sh
set -e

echo ">>> Creating S3 bucket..."
aws --endpoint-url http://moto:5555 s3 mb s3://email-templates --region us-east-1

echo ">>> Uploading email templates..."
aws --endpoint-url http://moto:5555 s3 sync /EmailTemplates s3://email-templates/templates/

echo ">>> Creating SQS dead letter queue..."
aws --endpoint-url http://moto:5555 sqs create-queue \
  --queue-name sidecar-create-account-dlq

echo ">>> Creating SQS main queue with redrive policy (maxReceiveCount=3)..."
aws --endpoint-url http://moto:5555 sqs create-queue \
  --queue-name sidecar-create-account-queue \
  --attributes '{
    "RedrivePolicy": "{\"deadLetterTargetArn\":\"arn:aws:sqs:us-east-1:123456789012:sidecar-create-account-dlq\",\"maxReceiveCount\":\"3\"}",
    "VisibilityTimeout": "30"
  }'

echo ">>> Creating SNS topic..."
aws --endpoint-url http://moto:5555 sns create-topic \
  --name sidecar-account-events

echo ">>> Subscribing SQS queue to SNS topic..."
aws --endpoint-url http://moto:5555 sns subscribe \
  --topic-arn arn:aws:sns:us-east-1:123456789012:sidecar-account-events \
  --protocol sqs \
  --notification-endpoint arn:aws:sqs:us-east-1:123456789012:sidecar-create-account-queue

echo ">>> All AWS resources initialized successfully"
