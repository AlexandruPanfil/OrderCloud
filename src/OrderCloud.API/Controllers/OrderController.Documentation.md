# Orders API Documentation

## Overview
The Orders API provides endpoints for managing orders in the OrderCloud system. Orders contain items, are associated with tenants and local users, and can optionally be linked to customers.

## Base URL
```
/api/Orders
```

## Authentication & Authorization

✅ **ENABLED**: This API requires authentication using one of the supported schemes.

### Supported Authentication Schemes

The Orders API uses **API Key Authentication** for all requests.

#### API Key Authentication

Used by all clients including the Blazor web application, mobile apps (MAUI), and third-party integrations.

**Authentication Headers:**
- `X-API-Key`: The tenant's API key
- `X-API-Secret`: The tenant's API secret

**Example Request:**
```bash
curl -X GET "https://localhost:7173/api/Orders" \
  -H "X-API-Key: your-tenant-api-key" \
  -H "X-API-Secret: your-tenant-api-secret"
```

These credentials are managed through the Tenants API and validated against the database.

**How Blazor App Authenticates:**
When a user logs into the Blazor application, the app automatically retrieves the API credentials for the user's associated tenant and includes them in all API requests. This happens transparently through the `ApiKeyHandler` - users don't need to manually provide API keys.

---

### Implementation Details

**API Configuration:**
```csharp
// API requires API key authentication
[Authorize(AuthenticationSchemes = "ApiKey")]
public class OrdersController : ControllerBase
```

**CORS Policy:**
The API is configured to accept requests from the Blazor app:
- `https://localhost:7067`
- `http://localhost:5067`

---

### Authorization Requirements

- ✅ All endpoints require API key authentication
- ✅ External clients must provide valid API key + secret headers
- ✅ Blazor app automatically includes API keys from the logged-in user's tenant
- No specific roles or policies are enforced
- Any authenticated tenant can access all endpoints

---

## Endpoints

### 1. Get All Orders
Retrieves all orders with their associated items.

**Endpoint:** `GET /api/Orders`

**Authentication:** Required (API Key)

**Response:**
- **200 OK** - Returns an array of orders
- **401 Unauthorized** - Authentication required or invalid credentials
- **Content-Type:** `application/json`

**Response Example:**
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "status": "Pending",
    "total": 150.50,
    "createdAt": "2024-01-15T10:30:00Z",
    "updatedAt": "2024-01-15T10:30:00Z",
    "tenantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "localUserId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "customerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "items": [
      {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "name": "Product Name",
        "price": 50.00,
        "quantity": 3,
        "tva": "19.0",
        "total": 150.00,
        "orderId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
      }
    ]
  }
]
```

---

### 2. Get Order by ID
Retrieves a specific order by its ID, including related customer, tenant, and local user information.

**Endpoint:** `GET /api/Orders/{id}`

**Authentication:** Required (API Key)

**Parameters:**
- `id` (GUID, required) - The unique identifier of the order

**Response:**
- **200 OK** - Returns the order
- **401 Unauthorized** - Authentication required or invalid credentials
- **404 Not Found** - Order not found
- **Content-Type:** `application/json`

**Response Example:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "status": "Pending",
  "total": 150.50,
  "createdAt": "2024-01-15T10:30:00Z",
  "updatedAt": "2024-01-15T10:30:00Z",
  "tenantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "localUserId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "customerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "customer": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "John Doe",
    "idno": "1234567890"
  },
  "tenant": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Tenant Name"
  },
  "localUser": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Local User",
    "pinCode": "****"
  },
  "items": [...]
}
```

---

### 3. Create Order
Creates a new order with items.

**Endpoint:** `POST /api/Orders`

**Authentication:** Required (API Key)

**Request Body:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "status": "Pending",
  "tenantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "localUserId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "customerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "customer": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "John Doe",
    "idno": "1234567890"
  },
  "localUser": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Local User",
    "pinCode": "1234"
  },
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "name": "Product Name",
      "price": 50.00,
      "quantity": 3,
      "tva": "19.0"
    }
  ]
}
```

**Minimal Request Body (recommended):**
```json
{
  "tenantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "localUser": {
    "name": "LocalUserName",
    "pinCode": "1234"
  },
  "items": [
    {
      "name": "Product Name",
      "price": 50.00,
      "quantity": 3,
      "tva": "19.0"
    }
  ]
}
```

**Field Requirements:**
- `tenantId` (GUID, required) - Must reference an existing tenant
- `localUserId` or `localUser` (required) - Must reference or identify an existing local user for the tenant
- `customerId` or `customer` (optional) - If provided with a name, a new customer will be created if it doesn't exist
- `items` (array, optional) - Order line items
- `id` (GUID, optional) - Auto-generated if not provided or if empty

**Business Logic:**
1. If `id` is empty or not provided, a new GUID is generated
2. Each item's `id` is auto-generated if empty
3. Item totals are calculated as `price * quantity`
4. Order total is the sum of all item totals
5. Timestamps (`createdAt`, `updatedAt`) are set to current UTC time
6. Tenant must exist in the database
7. Local user is resolved by:
   - Checking `localUserId` first
   - Then checking `localUser.Id`
   - Then checking by `localUser.Name` and `localUser.PinCode`
8. Customer is resolved or created:
   - If `customerId` is provided and exists, it's used
   - If `customer.Id` exists, it's used
   - If `customer.Name` is provided, a new customer is created
   - If no customer information is provided, the order is created without a customer

**Response:**
- **201 Created** - Order created successfully
- **400 Bad Request** - Invalid request body
- **401 Unauthorized** - Authentication required or invalid credentials
- **422 Unprocessable Entity** - Validation errors (tenant not found, local user not found, etc.)
- **500 Internal Server Error** - Database error
- **Content-Type:** `application/json`
- **Location Header:** `/api/Orders/{id}`

**Response Example:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "status": "Pending",
  "total": 150.00,
  "createdAt": "2024-01-15T10:30:00Z",
  "updatedAt": "2024-01-15T10:30:00Z",
  "tenantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "localUserId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "customerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "items": [...]
}
```

---

### 4. Update Order
Updates an existing order and its items.

**Endpoint:** `PUT /api/Orders/{id}`

**Authentication:** Required (API Key)

**Parameters:**
- `id` (GUID, required) - The unique identifier of the order to update

**Request Body:**
```json
{
  "status": "Completed",
  "localUserId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "customerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "name": "Updated Product Name",
      "price": 60.00,
      "quantity": 2,
      "tva": "19.0"
    }
  ]
}
```

**Business Logic:**
1. Updates order status, localUserId, and customerId
2. Updates `updatedAt` timestamp to current UTC time
3. For items:
   - Existing items (matched by `id`) are updated
   - New items (new `id` or empty `id`) are added
   - Items not included in the request are deleted
4. Item totals are recalculated as `price * quantity`
5. Order total is recalculated as the sum of all item totals
6. Validates tenant existence (via `PrepareOrderForSaveAsync`)
7. Validates local user existence for the tenant

**Response:**
- **200 OK** - Order updated successfully
- **400 Bad Request** - Invalid request body or ID
- **401 Unauthorized** - Authentication required or invalid credentials
- **404 Not Found** - Order not found
- **422 Unprocessable Entity** - Validation errors
- **500 Internal Server Error** - Database error
- **Content-Type:** `application/json`

**Response Example:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "status": "Completed",
  "total": 120.00,
  "createdAt": "2024-01-15T10:30:00Z",
  "updatedAt": "2024-01-15T11:45:00Z",
  "tenantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "localUserId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "customerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "items": [...]
}
```

---

### 5. Delete Order
Deletes an order and its associated items (cascade delete).

**Endpoint:** `DELETE /api/Orders/{id}`

**Authentication:** Required (API Key)

**Parameters:**
- `id` (GUID, required) - The unique identifier of the order to delete

**Response:**
- **204 No Content** - Order deleted successfully
- **401 Unauthorized** - Authentication required or invalid credentials
- **404 Not Found** - Order not found

---

## Data Models

### OrderDTO
```csharp
{
  "id": "guid",
  "status": "string",
  "total": "decimal",
  "createdAt": "datetime",
  "updatedAt": "datetime",
  "tenantId": "guid",
  "localUserId": "guid?",
  "customerId": "guid?",
  "tenant": "TenantDTO?",
  "localUser": "LocalUserDTO?",
  "customer": "CustomerDTO?",
  "items": "ItemDTO[]"
}
```

### ItemDTO
```csharp
{
  "id": "guid",
  "name": "string",
  "price": "decimal",
  "quantity": "decimal",
  "tva": "string",  // Note: TVA is stored as string, not decimal
  "total": "decimal",
  "orderId": "guid"
}
```

### CustomerDTO
```csharp
{
  "id": "guid",
  "name": "string",
  "idno": "string?"
}
```

### LocalUserDTO
```csharp
{
  "id": "guid",
  "name": "string",
  "pinCode": "string",
  "tenantId": "guid"
}
```

### TenantDTO
```csharp
{
  "id": "guid",
  "name": "string"
}
```

---

## Validation Rules

### Order Validation
- **TenantId** is required and must reference an existing tenant
- **LocalUser** is required (can be provided via `localUserId` or `localUser` object with Name and PinCode)
- **Customer** is optional; if provided with a name, will be created if it doesn't exist
- Empty GUIDs (`00000000-0000-0000-0000-000000000000`) are treated as null

### Item Validation
- Item `total` is automatically calculated as `price * quantity`
- Item `orderId` is automatically set to the parent order's ID

### Local User Resolution
The system attempts to find a local user in the following order:
1. By `localUserId` if provided and valid
2. By `localUser.Id` if provided and valid
3. By `localUser.Name` and `localUser.PinCode` combination

All local user lookups are scoped to the specified tenant.

### Customer Resolution
The system handles customers in the following way:
1. If `customerId` is provided and exists, uses that customer
2. If `customer.Id` is provided and exists, uses that customer and updates `customerId`
3. If `customer` object is provided with a name, creates a new customer
4. If no customer information is provided, order is created without a customer

---

## Error Responses

### 400 Bad Request
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400
}
```

### 401 Unauthorized
Returned when authentication is missing or invalid.

**For API Key Authentication:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Invalid API credentials"
}
```

**For Missing Authentication:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401
}
```

### 404 Not Found
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404
}
```

### 422 Unprocessable Entity (Validation Error)
```json
{
  "type": "https://tools.ietf.org/html/rfc4918#section-11.2",
  "title": "One or more validation errors occurred.",
  "status": 422,
  "errors": {
    "Tenant": ["Tenant not found."],
    "LocalUser": ["A valid local user is required for the selected tenant."]
  }
}
```

### 500 Internal Server Error
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "An error occurred while processing your request.",
  "status": 500,
  "detail": "Error message details"
}
```

---

## Notes

- **Authentication is required** for all endpoints
- All endpoints support cancellation tokens for async operations
- Navigation properties (Order reference in Items) are set to null in responses to prevent circular references
- All datetime values are stored and returned in UTC format
- The API uses Entity Framework Core with change tracking for updates
- Database operations are wrapped in try-catch blocks with proper error logging

---

## Troubleshooting

### Common Request Issues

#### 1. "The order field is required" Error

**Problem:** This error occurs when the JSON body isn't properly parsed by the model binder.

**Root Causes:**
- Missing or incorrect `Content-Type: application/json` header
- Invalid JSON syntax
- Unexpected property names (though the API now supports case-insensitive property names)
- Request body is empty or malformed

**Solutions:**

**✅ Check HTTP Headers:**
Ensure you include the correct header:
```
Content-Type: application/json
```

**✅ Validate JSON:**
Use a JSON validator (like jsonlint.com) to check your JSON is valid.

**✅ Use the Minimal Request:**
Start with the simplest possible request:
```json
{
  "tenantId": "YOUR-TENANT-GUID",
  "localUser": {
    "name": "UserName",
    "pinCode": "1234"
  }
}
```

**Example using curl:**
```bash
curl -X POST "https://localhost:7173/api/Orders" \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-api-key" \
  -H "X-API-Secret: your-api-secret" \
  -d '{
    "tenantId": "your-tenant-guid",
    "localUser": {
      "name": "UserName",
      "pinCode": "1234"
    },
    "items": [
      {
        "name": "Product",
        "price": 10.00,
        "quantity": 1,
        "tva": "19.0"
      }
    ]
  }'
```

**Example using Postman:**
1. Set method to **POST**
2. URL: `https://localhost:7173/api/Orders`
3. Headers tab:
   - `Content-Type`: `application/json`
   - `X-API-Key`: `your-api-key`
   - `X-API-Secret`: `your-api-secret`
4. Body tab: Select **raw** and **JSON** format
5. Paste the JSON request body

**Common Mistakes:**
- ❌ Forgetting `Content-Type` header
- ❌ Using `application/x-www-form-urlencoded` instead of `application/json`
- ❌ Trailing commas in JSON
- ❌ Single quotes instead of double quotes
- ❌ Sending raw text instead of JSON object

#### 2. TVA Type Mismatch Error

**Problem:** `The JSON value could not be converted to System.String` for `tva` field.

**Solution:** TVA must be a **string**, not a number:
- ✅ Correct: `"tva": "19.0"`
- ❌ Wrong: `"tva": 19.0`

#### 3. "Tenant not found" Validation Error

**Problem:** The provided `tenantId` doesn't exist in the database.

**Solutions:**
- Verify the tenant GUID is correct
- Use GET `/api/Tenants` to list available tenants
- Ensure the tenant hasn't been deleted

#### 4. "A valid local user is required" Error

**Problem:** The local user couldn't be found or matched.

**Solutions:**
- If using `localUserId`, ensure it exists and belongs to the specified tenant
- If using `localUser` object, provide valid `name` and `pinCode` that match a user in the database
- Check that the local user belongs to the correct tenant

#### 5. Authentication Failed (401 Unauthorized)

**Problem:** Missing or invalid authentication credentials.

**Solutions:**
- Ensure both `X-API-Key` and `X-API-Secret` headers are provided
- Verify the credentials match a tenant in the database
- Check for typos in header names (case-sensitive)

**For Blazor App Users:**
If you're getting 401 errors in the Blazor app:
1. Ensure you're logged in to the Blazor application
2. Verify your user account is associated with a tenant
3. Check that the tenant has valid API credentials configured
4. Check browser console for any errors in the `ApiKeyHandler`

---

## Security Considerations

1. **API Keys**: Store API keys securely. They are validated against the database for each request.
2. **HTTPS Only**: Always use HTTPS in production to protect credentials in transit.
3. **Tenant Isolation**: Each API key is associated with a specific tenant, providing tenant-level isolation.
4. **Automatic Authentication**: The Blazor app automatically retrieves and uses API credentials for authenticated users.
5. **CORS**: The API only accepts requests from configured origins.
