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
[Logic App (HTTP POST Callback)]
        |
        v
[Complete or Deadletter Processing]

```
---

## How It Works

### Order Placement
- A Logic App receives an HTTP POST request with order data.
- It forwards the order to a Service Bus queue (`orderqueue`).

### Order Processing
- An Azure Function with a **queue trigger** listens for new messages.
- The function performs validation on the order.
  - If **valid**, it triggers the "Complete" Logic App.
  - If **invalid**, it triggers the "Deadletter" Logic App.

---

## Azure Monitor Alerting Integration

This project includes an integrated alerting pipeline using Azure Monitor + Logic Apps:

### Triggered When:
- Function App emits a validation failure log (e.g. empty item name).

### What Happens:
1. **Azure Monitor Alert Rule** is set up on the Function App (based on logs).
2. The alert **triggers a Logic App**.
3. That Logic App sends an **email alert via Gmail** using `Send Email (V2)`.

---

## Monitoring, Logs & Telemetry

### Application Insights
- Application Insights is connected to the Function App.
- You can run custom KQL queries to inspect logs:

#### Example Queries:
```kusto
traces
| where timestamp > ago(15m)
| where message contains "Order Received"
| order by timestamp desc
```

## Repo Structure
```
/infra                -> Bicep infrastructure code
/queue-function       -> .NET isolated Azure Function
/.github/workflows    -> CI/CD workflows
logic-app-notes.md    -> Alerting setup
README.md
```

## Prerequisites
Azure CLI

Postman (or curl)

.NET 8 SDK

Azure Subscription

Gmail account (for Logic App connector)

---

## Deployment Steps
1. Deploy infrastructure with Bicep:
        az deployment group create \
  --resource-group <your-rg> \
  --template-file ./infra/main.bicep \
  --parameters ...
   
3. Push Function App code and trigger GitHub Actions workflow.

4. Use Postman or curl to send sample orders.

5. View logs in Application Insights and monitor alerts in Azure Monitor.


