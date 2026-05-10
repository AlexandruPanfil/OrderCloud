# API Authorization Summary

## All Controllers Now Secured with API Key Authentication

### ✅ Controllers with Authorization Applied

All API controllers now require **API Key authentication** (`X-API-Key` and `X-API-Secret` headers) to access their endpoints.

| Controller | Authorization | Special Notes |
|------------|--------------|---------------|
| **OrdersController** | ✅ `[Authorize(AuthenticationSchemes = "ApiKey")]` | Previously documented |
| **ItemsController** | ✅ `[Authorize(AuthenticationSchemes = "ApiKey")]` | Newly secured |
| **TenantsController** | ✅ `[Authorize(AuthenticationSchemes = "ApiKey")]` | **Exception:** POST (Create) endpoint has `[AllowAnonymous]` |
| **ApplicationUsersController** | ✅ `[Authorize(AuthenticationSchemes = "ApiKey")]` | Newly secured |
| **CustomersController** | ✅ `[Authorize(AuthenticationSchemes = "ApiKey")]` | Newly secured |
| **BillsController** | ✅ `[Authorize(AuthenticationSchemes = "ApiKey")]` | Newly secured |
| **LocalUsersController** | ✅ `[Authorize(AuthenticationSchemes = "ApiKey")]` | Newly secured |
| **DevicesController** | ✅ `[Authorize(AuthenticationSchemes = "ApiKey")]` | Newly secured |
| **DashboardController** | ✅ `[Authorize(AuthenticationSchemes = "ApiKey")]` | Newly secured |

---

## Special Case: Tenant Creation

### Why Tenant Create is AllowAnonymous

The **POST /api/Tenants** endpoint is the **only** endpoint that allows anonymous access because:

1. **Bootstrap Scenario**: New users/systems need to create their first tenant to get API credentials
2. **Self-Service**: Allows tenant creation without requiring existing credentials
3. **Security**: Once created, the tenant gets API credentials for all subsequent operations

**Endpoint:**
```http
POST /api/Tenants
Content-Type: application/json

{
  "name": "My Company",
  "applicationUserIds": ["user-guid-1", "user-guid-2"]
}
```

**Response includes:**
- `apiKey` - Use for X-API-Key header
- `apiSecret` - Use for X-API-Secret header (returned only once!)

**After tenant creation**, all other operations require authentication:
- GET /api/Tenants
- GET /api/Tenants/{id}
- PUT /api/Tenants/{id}
- DELETE /api/Tenants/{id}

---

## Authentication Requirements

### For External Clients (MAUI, Postman, etc.)

**Required Headers:**
```http
X-API-Key: your-tenant-api-key
X-API-Secret: your-tenant-api-secret
```

**Example (using curl):**
```bash
curl -X GET "https://localhost:7173/api/Items" \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-api-key" \
  -H "X-API-Secret: your-api-secret"
```

### For Blazor Application

**Automatic** - No manual headers needed!

The `ApiKeyHandler` automatically:
1. Detects authenticated Blazor user
2. Retrieves user's associated tenant
3. Adds API key headers to all requests
4. Works transparently

**Requirement:** User must be assigned to a tenant with valid API credentials.

---

## Testing Authorization

### Test 1: Without Authentication (Should Fail)
```bash
curl -X GET "https://localhost:7173/api/Items"
# Expected: 401 Unauthorized
```

### Test 2: With API Key (Should Succeed)
```bash
curl -X GET "https://localhost:7173/api/Items" \
  -H "X-API-Key: your-key" \
  -H "X-API-Secret: your-secret"
# Expected: 200 OK with data
```

### Test 3: Create Tenant Without Auth (Should Succeed)
```bash
curl -X POST "https://localhost:7173/api/Tenants" \
  -H "Content-Type: application/json" \
  -d '{"name":"Test Tenant"}'
# Expected: 201 Created with API credentials
```

### Test 4: Get Tenants Without Auth (Should Fail)
```bash
curl -X GET "https://localhost:7173/api/Tenants"
# Expected: 401 Unauthorized
```

---

## Security Benefits

✅ **Comprehensive Protection** - All API endpoints are secured  
✅ **Tenant Isolation** - Each tenant has unique credentials  
✅ **Audit Trail** - All requests are tied to a tenant  
✅ **Self-Service Onboarding** - New tenants can bootstrap themselves  
✅ **Consistent Mechanism** - All clients use same authentication  

---

## Troubleshooting

### Getting 401 Errors in Blazor?

**Check:**
1. ✅ User is logged in
2. ✅ User is associated with a tenant (see `SQL_DIAGNOSTICS.sql`)
3. ✅ Tenant has valid API credentials
4. ✅ Browser console for `ApiKeyHandler` errors

**Fix:**
- Run diagnostic queries from `SQL_DIAGNOSTICS.sql`
- Assign user to tenant via Blazor UI
- Verify tenant has `ApiKey` and `ApiSecret` in database

### Getting 401 Errors from External Client?

**Check:**
1. ✅ Headers are exactly `X-API-Key` and `X-API-Secret` (case-sensitive)
2. ✅ Values match tenant credentials in database
3. ✅ `Content-Type: application/json` header is included
4. ✅ Not trying to access the Tenant Create endpoint (it doesn't need auth)

---

## Next Steps

1. **Test all endpoints** - Verify authorization works for each controller
2. **Document API keys** - Ensure users know where to get credentials
3. **Monitor logs** - Watch for authentication failures
4. **Consider rate limiting** - Prevent API key abuse
5. **Implement key rotation** - Allow tenants to regenerate credentials periodically

---

## Files Modified

✅ `OrderCloud.API/Controllers/OrderController.cs`  
✅ `OrderCloud.API/Controllers/ItemsController.cs`  
✅ `OrderCloud.API/Controllers/TenantsController.cs`  
✅ `OrderCloud.API/Controllers/ApplicationUsersController.cs`  
✅ `OrderCloud.API/Controllers/CustomersController.cs`  
✅ `OrderCloud.API/Controllers/BillsController.cs`  
✅ `OrderCloud.API/Controllers/LocalUsersController.cs`  
✅ `OrderCloud.API/Controllers/DevicesController.cs`  
✅ `OrderCloud.API/Controllers/DashboardController.cs`  
