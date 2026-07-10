#!/bin/bash
set -euo pipefail

# Bootstrap Amazon Linux 2023 for CLS Budget API (Docker + app directory).
dnf update -y
dnf install -y docker
systemctl enable docker
systemctl start docker
usermod -aG docker ec2-user

mkdir -p /opt/cls-budget-api
chown ec2-user:ec2-user /opt/cls-budget-api

echo "CLS Budget API host ready." > /var/log/cls-budget-api-bootstrap.log
