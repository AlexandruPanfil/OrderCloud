# API Authentication Quick Reference

## 🔐 Authentication Status

**All API endpoints require API Key authentication**  
**Exception:** `POST /api/Tenants` (tenant creation) is public

---

## 🚀 Quick Start

### Step 1: Create a Tenant (No Auth Required)
```bash
POST https://localhost:7173/api/Tenants
Content-Type: application/json

{
  "name": "My Company"
}
```

**Response (save these!):**
```json
{
  "id": "...",
  "name": "My Company",
  "apiKey": "abc123...",      ← Use for X-API-Key
  "apiSecret": "xyz789..."    ← Use for X-API-Secret (shown only once!)
}
```

### Step 2: Use API Key for All Other Requests
```bash
GET https://localhost:7173/api/Orders
X-API-Key: abc123...
X-API-Secret: xyz789...
```

---

## 📋 All Secured Endpoints

### Catalog Items
- `GET    /api/Items` - List all items
- `GET    /api/Items/{id}` - Get item by ID
- `POST   /api/Items` - Create item
- `PUT    /api/Items/{id}` - Update item
- `DELETE /api/Items/{id}` - Delete item

### Orders
- `GET    /api/Orders` - List all orders
- `GET    /api/Orders/{id}` - Get order by ID
- `POST   /api/Orders` - Create order
- `PUT    /api/Orders/{id}` - Update order
- `DELETE /api/Orders/{id}` - Delete order

### Customers
- `GET    /api/Customers` - List all customers
- `GET    /api/Customers/{id}` - Get customer by ID
- `POST   /api/Customers` - Create customer
- `PUT    /api/Customers/{id}` - Update customer
- `DELETE /api/Customers/{id}` - Delete customer

### Bills
- `GET    /api/Bills` - List all bills
- `GET    /api/Bills/{id}` - Get bill by ID
- `POST   /api/Bills` - Create bill
- `PUT    /api/Bills/{id}` - Update bill
- `DELETE /api/Bills/{id}` - Delete bill

### Local Users
- `GET    /api/LocalUsers` - List all local users
- `GET    /api/LocalUsers/{id}` - Get local user by ID
- `POST   /api/LocalUsers` - Create local user
- `PUT    /api/LocalUsers/{id}` - Update local user
- `DELETE /api/LocalUsers/{id}` - Delete local user

### Devices
- `GET    /api/Devices` - List all devices
- `GET    /api/Devices/{id}` - Get device by ID
- `POST   /api/Devices` - Create device
- `PUT    /api/Devices/{id}` - Update device
- `DELETE /api/Devices/{id}` - Delete device

### Tenants (Except POST)
- `GET    /api/Tenants` - List all tenants (🔒 Auth Required)
- `GET    /api/Tenants/{id}` - Get tenant by ID (🔒 Auth Required)
- `POST   /api/Tenants` - Create tenant (🔓 Public)
- `PUT    /api/Tenants/{id}` - Update tenant (🔒 Auth Required)
- `DELETE /api/Tenants/{id}` - Delete tenant (🔒 Auth Required)

### Application Users
- `GET    /api/ApplicationUsers` - List all users
- `GET    /api/ApplicationUsers/{id}` - Get user by ID
- `POST   /api/ApplicationUsers` - Create user
- `PUT    /api/ApplicationUsers/{id}` - Update user
- `DELETE /api/ApplicationUsers/{id}` - Delete user

### Dashboard
- `GET    /api/Dashboard` - Get dashboard stats

---

## 🔧 cURL Examples

### Without Authentication (Fails ❌)
```bash
curl https://localhost:7173/api/Items
# Response: 401 Unauthorized
```

### With Authentication (Works ✅)
```bash
curl https://localhost:7173/api/Items \
  -H "X-API-Key: your-api-key" \
  -H "X-API-Secret: your-api-secret"
# Response: 200 OK
```

### Create Resource
```bash
curl -X POST https://localhost:7173/api/Items \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-api-key" \
  -H "X-API-Secret: your-api-secret" \
  -d '{
    "name": "Product Name",
    "price": 19.99,
    "tva": "19.0",
    "tenantId": "your-tenant-guid"
  }'
```

---

## 💻 Code Examples

### C# (HttpClient)
```csharp
var client = new HttpClient();
client.DefaultRequestHeaders.Add("X-API-Key", "your-api-key");
client.DefaultRequestHeaders.Add("X-API-Secret", "your-api-secret");

var response = await client.GetAsync("https://localhost:7173/api/Items");
var items = await response.Content.ReadFromJsonAsync<List<ItemDTO>>();
```

### JavaScript (Fetch)
```javascript
fetch('https://localhost:7173/api/Items', {
  headers: {
    'X-API-Key': 'your-api-key',
    'X-API-Secret': 'your-api-secret',
    'Content-Type': 'application/json'
  }
})
.then(response => response.json())
.then(data => console.log(data));
```

### PowerShell
```powershell
$headers = @{
    'X-API-Key' = 'your-api-key'
    'X-API-Secret' = 'your-api-secret'
}

Invoke-RestMethod -Uri 'https://localhost:7173/api/Items' `
                  -Headers $headers
```

---

## 🎯 Blazor Users

**No manual authentication needed!**

When you log into the Blazor app:
1. ✅ You're automatically authenticated
2. ✅ `ApiKeyHandler` retrieves your tenant's credentials
3. ✅ All API calls include authentication headers
4. ✅ Just use the UI normally

**Requirement:** Your user account must be associated with a tenant.

**Check your tenant assignment:**
```sql
-- Run in SQL Server Management Studio
SELECT u.UserName, t.Name, t.ApiKey
FROM AspNetUsers u
JOIN TenantApplicationUser tau ON u.Id = tau.ApplicationUsersId
JOIN Tenants t ON tau.TenantsId = t.Id
WHERE u.UserName = 'your-username'
```

---

## 🐛 Troubleshooting

| Error | Cause | Solution |
|-------|-------|----------|
| 401 Unauthorized | Missing auth headers | Add `X-API-Key` and `X-API-Secret` |
| 401 Unauthorized | Invalid credentials | Verify key/secret match database |
| 401 in Blazor | User not assigned to tenant | Run `SQL_DIAGNOSTICS.sql` queries |
| 401 in Blazor | Tenant has no API credentials | Regenerate tenant API keys |
| 400 Bad Request | Missing `Content-Type` | Add `Content-Type: application/json` |
| 422 Validation Error | Invalid data | Check request body against model |

---

## 📚 Documentation Files

- `API_CONTROLLERS_AUTHORIZATION.md` - Complete controller authorization overview
- `OrderController.Documentation.md` - Detailed Orders API documentation
- `BLAZOR_401_FIX.md` - Troubleshooting Blazor authentication
- `AUTHORIZATION_IMPLEMENTATION.md` - Technical implementation details
- `SQL_DIAGNOSTICS.sql` - Database diagnostic queries

---

## 🔒 Security Notes

1. **Store credentials securely** - Never commit API keys to source control
2. **Use HTTPS** - Always use secure connections in production
3. **Rotate keys** - Periodically regenerate API credentials
4. **Monitor usage** - Watch for suspicious authentication patterns
5. **Tenant isolation** - Each tenant's data is isolated by their API key
