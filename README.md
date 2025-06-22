# Azure Order Processing

This is a beginner-friendly Azure integration project that demonstrates serverless order processing using multiple Azure Integration Services. It aligns with real-world junior cloud engineer requirements and is fully infrastructure-as-code driven using Bicep.

---

## What This Project Covers

- Azure Function Apps
- Azure Logic Apps
- Azure Service Bus
- Azure Key Vault
- API Management
- Infrastructure as Code (Bicep)
- CI/CD readiness (via GitHub/Azure DevOps)

---

## Architecture Overview

```text
[Client (Postman / Frontend)]
        |
        v
[API Management]
        |
        v
[Logic App (HTTP Trigger)]
        |
        v
[Azure Service Bus Queue: "orderqueue"]
        |
        v
[Azure Function (Queue Trigger)]
        |
        v
[Storage/Table/Log Output or Custom Logic]
