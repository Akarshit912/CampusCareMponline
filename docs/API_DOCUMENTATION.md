# CampusCare REST Web API Documentation

This document provides the complete API reference for the **CampusCare Web API** (`CampusCare.WebAPI`), including endpoint descriptions, request/response models, query parameters, cURL examples, error codes, and external webhook integrations.

---

## 1. Overview & Swagger UI

- **API Framework**: ASP.NET Core 10.0 Web API
- **Default Base URL (Local Development)**: `http://localhost:5001` or `https://localhost:7001`
- **Interactive Swagger UI**: `http://localhost:5001/` (Mapped directly to root URL)
- **OpenAPI JSON Specification**: `http://localhost:5001/swagger/v1/swagger.json`
- **Data Format**: `application/json`

---

## 2. API Endpoints Summary

| HTTP Method | Route | Description | Auth Required |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/complaints` | Retrieve complaints with optional status and department filters | Public / Token |
| `GET` | `/api/complaints/{id}` | Retrieve complaint details, AI triage results, and audit history | Public / Token |
| `POST` | `/api/complaints/escalate-overdue` | Trigger SLA escalation check for overdue complaints (> 48h) | Public / Service |
| `POST` | `/api/complaints/n8n/webhook-callback`| Receive incoming webhook callbacks from n8n workflows | Public / Webhook |
| `DELETE` | `/api/complaints/{id}` | Permanently delete a complaint record and its history | Admin / Token |

---

## 3. Detailed Endpoint Reference

### 3.1 Get All Complaints

`GET /api/complaints`

Retrieves a JSON array of complaints with summary metadata. Supports filtering by status and department.

#### Query Parameters

| Parameter | Type | Required | Description | Example |
| :--- | :--- | :--- | :--- | :--- |
| `status` | `string` | No | Filter by complaint status name (`Submitted`, `Assigned`, `InProgress`, `Escalated`, `Resolved`, `Closed`, `Rejected`) | `InProgress` |
| `departmentId` | `int` | No | Filter by department integer ID | `1` |

#### Request Example (cURL)
```bash
curl -X GET "http://localhost:5001/api/complaints?status=InProgress&departmentId=1" \
     -H "Accept: application/json"
```

#### Response Example (`200 OK`)
```json
[
  {
    "id": 1,
    "complaintNumber": "CMP-2026-00001",
    "title": "Wi-Fi not working in Computer Lab 3",
    "location": "Computer Lab 3, CS Block",
    "status": "InProgress",
    "priority": "High",
    "category": "IT / Wi-Fi",
    "department": "Information Technology",
    "student": "John Student",
    "assignedStaff": "Alex Staff (IT Tech)",
    "createdAt": "2026-08-20T10:15:30Z",
    "isEscalated": false
  },
  {
    "id": 2,
    "complaintNumber": "CMP-2026-00002",
    "title": "Water leakage in Hostel Block B washroom",
    "location": "Hostel B, 2nd Floor",
    "status": "Submitted",
    "priority": "Medium",
    "category": "Maintenance",
    "department": "Facility Maintenance",
    "student": "Emma Student",
    "assignedStaff": null,
    "createdAt": "2026-08-22T08:30:00Z",
    "isEscalated": false
  }
]
```

---

### 3.2 Get Complaint Details by ID

`GET /api/complaints/{id}`

Retrieves complete details for a single complaint, including the associated AI analysis, full audit timeline history, and resolution notes.

#### Path Parameters

| Parameter | Type | Required | Description |
| :--- | :--- | :--- | :--- |
| `id` | `int` | Yes | Unique ID of the complaint record |

#### Request Example (cURL)
```bash
curl -X GET "http://localhost:5001/api/complaints/1" \
     -H "Accept: application/json"
```

#### Response Example (`200 OK`)
```json
{
  "id": 1,
  "complaintNumber": "CMP-2026-00001",
  "title": "Wi-Fi not working in Computer Lab 3",
  "description": "The access point in Lab 3 is flashing orange. No devices can connect to the campus SSID.",
  "location": "Computer Lab 3, CS Block",
  "status": "InProgress",
  "priority": "High",
  "category": "IT / Wi-Fi",
  "department": "Information Technology",
  "student": "John Student",
  "assignedStaff": "Alex Staff (IT Tech)",
  "aiSummary": "Wi-Fi down in Lab 3 - The access point in Lab 3 is flashing orange",
  "aiSuggestedPriority": "High",
  "aiSuggestedCategory": "IT / Wi-Fi",
  "resolutionDetails": null,
  "createdAt": "2026-08-20T10:15:30Z",
  "resolvedAt": null,
  "history": [
    {
      "action": "Complaint Filed",
      "status": "Submitted",
      "timestamp": "2026-08-20T10:15:30Z",
      "user": "John Student",
      "notes": "Initial complaint submission by student."
    },
    {
      "action": "Staff Assigned",
      "status": "Assigned",
      "timestamp": "2026-08-20T11:00:00Z",
      "user": "IT Department Manager",
      "notes": "Assigned to Alex Staff (IT Tech)."
    },
    {
      "action": "Work Started",
      "status": "InProgress",
      "timestamp": "2026-08-20T11:30:00Z",
      "user": "Alex Staff (IT Tech)",
      "notes": "Diagnosing POE switch power issue."
    }
  ]
}
```

#### Error Response (`404 Not Found`)
```json
{
  "message": "Complaint ID 999 not found."
}
```

---

### 3.3 Trigger SLA Escalation Check

`POST /api/complaints/escalate-overdue`

Triggers an on-demand scan of all active complaints. Any complaint older than `overdueHours` (default: 48) that remains unresolved is automatically escalated to `Escalated` status, flagged in audit logs, and sent to the notification pipeline.

#### Query Parameters

| Parameter | Type | Default | Description |
| :--- | :--- | :--- | :--- |
| `overdueHours` | `int` | `48` | Threshold in hours after which an unresolved complaint is considered breached |

#### Request Example (cURL)
```bash
curl -X POST "http://localhost:5001/api/complaints/escalate-overdue?overdueHours=48" \
     -H "Accept: application/json"
```

#### Response Example (`200 OK`)
```json
{
  "success": true,
  "message": "Escalation scan completed. 2 overdue complaints escalated.",
  "escalatedCount": 2,
  "timestamp": "2026-08-24T10:45:00.1234567Z"
}
```

---

### 3.4 n8n Webhook Callback Receiver

`POST /api/complaints/n8n/webhook-callback`

Receives webhook callback payloads from automated n8n workflows (e.g., ticket resolution confirmation, WhatsApp/SMS gateway status, or external escalation triggers).

#### Request Body Schema
```json
{
  "event": "TicketDelivered",
  "complaintId": 1,
  "externalService": "TwilioWhatsAppGateway",
  "status": "Delivered",
  "deliveredAt": "2026-08-24T10:45:00Z"
}
```

#### Request Example (cURL)
```bash
curl -X POST "http://localhost:5001/api/complaints/n8n/webhook-callback" \
     -H "Content-Type: application/json" \
     -d '{"event":"TicketDelivered","complaintId":1,"status":"Delivered"}'
```

#### Response Example (`200 OK`)
```json
{
  "status": "Acknowledged",
  "timestamp": "2026-08-24T10:45:01.0000000Z"
}
```

---

### 3.5 Delete Complaint

`DELETE /api/complaints/{id}`

Permanently removes a complaint record along with all associated history, comments, attachments, AI analyses, and feedback.

#### Path Parameters

| Parameter | Type | Required | Description |
| :--- | :--- | :--- | :--- |
| `id` | `int` | Yes | Unique ID of the complaint to delete |

#### Request Example (cURL)
```bash
curl -X DELETE "http://localhost:5001/api/complaints/5" \
     -H "Accept: application/json"
```

#### Response Example (`200 OK`)
```json
{
  "success": true,
  "message": "Complaint 'CMP-2026-00005' (ID: 5) permanently deleted.",
  "timestamp": "2026-08-24T10:50:00.0000000Z"
}
```

#### Error Response (`404 Not Found`)
```json
{
  "success": false,
  "message": "Complaint ID 999 not found."
}
```

---

## 4. HTTP Status Codes & Error Handling

The API adheres to standard HTTP status codes:

| Status Code | Reason | Description |
| :--- | :--- | :--- |
| `200 OK` | Success | The request completed successfully and returns data. |
| `400 Bad Request` | Validation Error | Missing or invalid request parameters or body. |
| `401 Unauthorized` | Authentication Missing | Missing credentials for protected endpoints. |
| `403 Forbidden` | Access Denied | User does not have sufficient role privileges. |
| `404 Not Found` | Resource Not Found | Specified complaint ID does not exist in the database. |
| `500 Internal Server Error` | Server Error | Unhandled server exception (logged to console/log file). |
