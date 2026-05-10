-- Diagnostic Query: Check User-Tenant Configuration
-- Run this to verify users are properly associated with tenants

-- 1. Check if user exists and is associated with tenants
SELECT 
    u.Id AS UserId,
    u.UserName,
    u.Email,
    t.Id AS TenantId,
    t.Name AS TenantName,
    CASE 
        WHEN t.ApiKey IS NULL OR t.ApiKey = '' THEN 'MISSING'
        ELSE 'OK'
    END AS ApiKeyStatus,
    CASE 
        WHEN t.ApiSecret IS NULL OR t.ApiSecret = '' THEN 'MISSING'
        ELSE 'OK'
    END AS ApiSecretStatus
FROM 
    AspNetUsers u
LEFT JOIN 
    TenantApplicationUser tau ON u.Id = tau.ApplicationUsersId
LEFT JOIN 
    Tenants t ON tau.TenantsId = t.Id
WHERE 
    u.UserName = 'YOUR-USERNAME-HERE'  -- Replace with your username
ORDER BY 
    t.Name;

-- 2. List all users and their tenant assignments
SELECT 
    u.UserName,
    u.Email,
    COUNT(DISTINCT t.Id) AS TenantCount,
    STRING_AGG(t.Name, ', ') AS AssignedTenants
FROM 
    AspNetUsers u
LEFT JOIN 
    TenantApplicationUser tau ON u.Id = tau.ApplicationUsersId
LEFT JOIN 
    Tenants t ON tau.TenantsId = t.Id
GROUP BY 
    u.UserName, u.Email
ORDER BY 
    u.UserName;

-- 3. List all tenants with their API credential status
SELECT 
    t.Id,
    t.Name,
    CASE 
        WHEN t.ApiKey IS NULL OR t.ApiKey = '' THEN 'MISSING - NEEDS REGENERATION'
        ELSE 'OK'
    END AS ApiKeyStatus,
    CASE 
        WHEN t.ApiSecret IS NULL OR t.ApiSecret = '' THEN 'MISSING - NEEDS REGENERATION'
        ELSE 'OK'
    END AS ApiSecretStatus,
    COUNT(DISTINCT tau.ApplicationUsersId) AS UserCount
FROM 
    Tenants t
LEFT JOIN 
    TenantApplicationUser tau ON t.Id = tau.TenantsId
GROUP BY 
    t.Id, t.Name, t.ApiKey, t.ApiSecret
ORDER BY 
    t.Name;

-- 4. Find users without any tenant assignment
SELECT 
    u.Id,
    u.UserName,
    u.Email,
    'NO TENANT ASSIGNED - WILL GET 401 ERRORS' AS Status
FROM 
    AspNetUsers u
LEFT JOIN 
    TenantApplicationUser tau ON u.Id = tau.ApplicationUsersId
WHERE 
    tau.ApplicationUsersId IS NULL;

-- 5. Find tenants without API credentials
SELECT 
    t.Id,
    t.Name,
    'API CREDENTIALS MISSING - REGENERATE NEEDED' AS Status
FROM 
    Tenants t
WHERE 
    t.ApiKey IS NULL 
    OR t.ApiKey = '' 
    OR t.ApiSecret IS NULL 
    OR t.ApiSecret = '';
