# Logic App Integration Notes

This document explains my Logic Apps are integrated into the Azure Order Processing project for workflow automation and alerting. It also includes configuration steps, alerting logic, and testing procedures.

---

## Logic App 1: Order Processing

### Trigger
- **When an HTTP request is received**
- Trigger URL is stored in the Function App as an environment variable:
  - `LogicAppUrl`

### Actions
1. **Send HTTP Request to Order Settlement API** (or another service)
2. **Log response in Azure Monitor**

### Purpose
Handles all valid order submissions from the Azure Function. It is only invoked after the function passes validation.

---

## Logic App 2: Alerting on Validation Failures

### Trigger
- **When an HTTP request is received**
- Trigger URL stored as:
  - `AlertLogicAppUrl`

### Actions
1. **Send Email (V2)** using Gmail connector
   - Subject: Order Validation Failed
   - Body: Includes order details and failure reason

### Purpose
Alerts the developer/admin when an order has missing or invalid data (e.g., missing item name or quantity = 0).

---

## Environment Variables

| Variable                             | Description                              |
|--------------------------------------|------------------------------------------|
| `LogicAppUrl`                        | URL for the primary order logic app      |
| `AlertLogicAppUrl`                   | URL for the alerting logic app           |
| `orderservicebusjiga1234_SERVICEBUS` | URL for 

Set these in `local.settings.json` for local development or in Azure Function App configuration.

---

## Testing the Logic Apps

### Valid Order Example (Triggers Logic App 1)
```json
{
  "orderId": 101,
  "item": "jollof rice",
  "quantity": 3
}

```
- Function logs: Order Received - ID: 101, Item: jollof rice, Quantity: 3
- Logic App 1 is invoked
- No alert is sent

### Invalid Order Example (Triggers Logic App 2)
```json
{
  "orderId": 102,
  "item": "",
  "quantity": 0
}
```

- Function logs: Validation failed - Item: '', Quantity: 0
- Logic App 2 is invoked
- Admin receives an alert email 

## Alerting Logic Flow
Function App
   ↓
If order.Item is null/empty OR order.Quantity <= 0
   ↓
→ Dead-letter the message
→ Log warning
→ POST alert to AlertLogicAppUrl


