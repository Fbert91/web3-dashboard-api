# DEPLOYMENT.md - Production Deployment Guide

## Pre-Deployment Security Checklist

Before deploying to production, complete all items:

- [ ] Generate secure API keys (use strong random strings)
- [ ] Configure CORS with your domain(s)
- [ ] Set up HTTPS certificates
- [ ] Configure rate limiting appropriately
- [ ] Review logging configuration
- [ ] Set up monitoring and alerting
- [ ] Test circuit breaker behavior
- [ ] Validate all input validation rules
- [ ] Back up your database
- [ ] Create deployment runbook

## Environment Configuration

### 1. API Keys Setup

Generate strong API keys:
```bash
# Option 1: Use a password generator
openssl rand -hex 32
# Output: a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6

# Option 2: Use .NET's SecureString equivalent
python -c "import secrets; print(secrets.token_hex(32))"
```

Update `appsettings.Production.json`:
```json
{
  "ApiKeys": {
    "Valid": {
      "a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6": "app-frontend",
      "b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6q7": "app-backend",
      "c3d4e5f6g7h8i9j0k1l2m3n4o5p6q7r8": "app-mobile"
    }
  }
}
```

### 2. Rate Limiting Configuration

For production, adjust rate limits based on expected load:

```json
{
  "RateLimiting": {
    "Enabled": true,
    "RequestsPerWindow": 1000,      // Requests per window
    "WindowDurationSeconds": 60     // 60 seconds
  }
}
```

**Recommendations:**
- **Public API**: 100 requests/min per IP
- **Authenticated Users**: 1000 requests/min per API key
- **Internal Services**: Unlimited (IP-based whitelist)

### 3. CORS Configuration

**Development:**
```json
{
  "Cors": {
    "AllowedOrigins": ["http://localhost:3000", "http://localhost:5000"]
  }
}
```

**Production:**
```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://yourdomain.com",
      "https://app.yourdomain.com",
      "https://dashboard.yourdomain.com"
    ]
  }
}
```

### 4. Logging Configuration

**Production logging** (appsettings.Production.json):
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning",
      "Web3DashboardAPI": "Information"
    }
  }
}
```

### 5. RPC Endpoint Configuration

Use a dedicated RPC provider:

```json
{
  "Ethereum": {
    "RpcUrl": "https://eth-mainnet.your-provider.com/v1/YOUR_KEY",
    "NetworkName": "Ethereum Mainnet",
    "ChainId": 1
  }
}
```

**Recommended Providers:**
- Infura: https://infura.io
- Alchemy: https://www.alchemy.com
- QuickNode: https://www.quicknode.com
- Ankr: https://www.ankr.com

## Deployment Methods

### Option 1: Docker Deployment (Recommended)

Create `Dockerfile`:
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/Web3DashboardAPI.API/", "src/Web3DashboardAPI.API/"]
COPY ["src/Web3DashboardAPI.Application/", "src/Web3DashboardAPI.Application/"]
COPY ["src/Web3DashboardAPI.Domain/", "src/Web3DashboardAPI.Domain/"]
COPY ["src/Web3DashboardAPI.Infrastructure/", "src/Web3DashboardAPI.Infrastructure/"]

# Build
WORKDIR "/src/src/Web3DashboardAPI.API"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD dotnet /app/healthcheck.dll || exit 1

EXPOSE 5000 5001
ENTRYPOINT ["dotnet", "Web3DashboardAPI.API.dll"]
```

Build and run:
```bash
# Build
docker build -t web3-dashboard-api:2.0 .

# Run
docker run -d \
  -p 5000:5000 \
  -p 5001:5001 \
  -e "ASPNETCORE_ENVIRONMENT=Production" \
  -e "Ethereum:RpcUrl=https://..." \
  -v /path/to/appsettings.Production.json:/app/appsettings.Production.json \
  -v web3-dashboard-data:/app/data \
  --name web3-dashboard-api \
  web3-dashboard-api:2.0
```

Docker Compose (recommended):
```yaml
version: '3.8'
services:
  api:
    image: web3-dashboard-api:2.0
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      Ethereum__RpcUrl: ${ETH_RPC_URL}
    ports:
      - "5000:5000"
      - "5001:5001"
    volumes:
      - web3-dashboard-data:/app/data
      - ./appsettings.Production.json:/app/appsettings.Production.json:ro
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5000/api/health"]
      interval: 30s
      timeout: 3s
      retries: 3

volumes:
  web3-dashboard-data:
```

### Option 2: Linux Service Deployment

Create `/etc/systemd/system/web3-dashboard.service`:
```ini
[Unit]
Description=Web3 Dashboard API
After=network.target
StartLimitIntervalSec=0

[Service]
Type=notify
User=www-data
WorkingDirectory=/opt/web3-dashboard
Environment="ASPNETCORE_ENVIRONMENT=Production"
Environment="Ethereum__RpcUrl=https://..."
ExecStart=/usr/bin/dotnet /opt/web3-dashboard/Web3DashboardAPI.API.dll
Restart=always
RestartSec=10

[Install]
WantedBy=multi-user.target
```

Deploy:
```bash
# Copy application
sudo cp -r ./publish /opt/web3-dashboard

# Set permissions
sudo chown -R www-data:www-data /opt/web3-dashboard

# Copy configuration
sudo cp appsettings.Production.json /opt/web3-dashboard/

# Enable service
sudo systemctl daemon-reload
sudo systemctl enable web3-dashboard
sudo systemctl start web3-dashboard

# Check status
sudo systemctl status web3-dashboard
```

### Option 3: Azure App Service Deployment

```bash
# Login to Azure
az login

# Create resource group
az group create -n web3-dashboard-rg -l eastus

# Create App Service plan
az appservice plan create -g web3-dashboard-rg -n web3-dashboard-plan \
  --sku B2 --is-linux

# Create web app
az webapp create -g web3-dashboard-rg -p web3-dashboard-plan -n web3-dashboard-api

# Deploy
dotnet publish -c Release -o ./publish
cd publish
zip -r ../web3-dashboard-api.zip .
az webapp deployment source config-zip -g web3-dashboard-rg \
  -n web3-dashboard-api --src ../web3-dashboard-api.zip

# Configure environment
az webapp config appsettings set -g web3-dashboard-rg \
  -n web3-dashboard-api \
  --settings "Ethereum:RpcUrl=$ETH_RPC_URL" \
  "ASPNETCORE_ENVIRONMENT=Production"
```

### Option 4: AWS ECS Deployment

```bash
# Create ECR repository
aws ecr create-repository --repository-name web3-dashboard-api

# Build and push image
docker build -t web3-dashboard-api:2.0 .
docker tag web3-dashboard-api:2.0 \
  ACCOUNT_ID.dkr.ecr.REGION.amazonaws.com/web3-dashboard-api:2.0
aws ecr get-login-password --region REGION | docker login --username AWS \
  --password-stdin ACCOUNT_ID.dkr.ecr.REGION.amazonaws.com
docker push ACCOUNT_ID.dkr.ecr.REGION.amazonaws.com/web3-dashboard-api:2.0

# Create ECS task definition
aws ecs register-task-definition --cli-input-json file://task-definition.json

# Deploy service
aws ecs create-service --cluster web3-cluster --service-name web3-api \
  --task-definition web3-dashboard-api:1 --desired-count 2
```

## Post-Deployment Verification

### 1. Health Check
```bash
curl http://localhost:5000/api/health
# Response: {"status":"healthy","timestamp":"2024-02-19T10:30:00Z"}
```

### 2. API Info
```bash
curl http://localhost:5000/api/info
# Response: {"name":"Web3 Dashboard API","version":"2.0",...}
```

### 3. Authentication Test
```bash
# Without API key (should fail)
curl http://localhost:5000/api/wallets/0x...
# Response: 401 Unauthorized

# With API key (should work)
curl -H "X-API-Key: your-api-key" http://localhost:5000/api/wallets/0x...
# Response: 200 OK
```

### 4. Rate Limit Test
```bash
# Run 101 requests quickly
for i in {1..101}; do
  curl -H "X-API-Key: test-key" http://localhost:5000/api/health
done

# Request 101 should get 429 Too Many Requests
```

## Monitoring & Alerting

### Application Insights (Azure)

```csharp
// In Program.cs
builder.Services.AddApplicationInsightsTelemetry();
```

### Datadog

```bash
# Install agent
DD_AGENT_MAJOR_VERSION=7 DD_API_KEY=$DATADOG_API_KEY \
  DD_SITE="datadoghq.com" bash -c "$(curl -L https://s3.amazonaws.com/dd-agent/scripts/install_agent.sh)"
```

### Prometheus

```bash
# Add prometheus scraping endpoint
# Configure metrics collection
```

### CloudWatch (AWS)

```json
{
  "MetricFilters": [
    {
      "filterName": "AuthenticationFailures",
      "filterPattern": "[..., status=\"401\", ...]",
      "metricTransformations": [{
        "metricName": "AuthFailures",
        "metricNamespace": "Web3Dashboard",
        "metricValue": "1"
      }]
    }
  ]
}
```

## Backup & Recovery

### Database Backup

```bash
# Daily backup
0 2 * * * sqlite3 /path/to/Web3Dashboard.db ".backup /backups/web3-$(date +\%Y\%m\%d).db"

# Keep 30 days of backups
find /backups -name "web3-*.db" -mtime +30 -delete
```

### Configuration Backup

```bash
# Backup configuration files
cp /opt/web3-dashboard/appsettings.Production.json /backups/config-$(date +%Y%m%d).json

# Encrypt sensitive data
openssl enc -aes-256-cbc -in /backups/config-$(date +%Y%m%d).json \
  -out /backups/config-$(date +%Y%m%d).json.enc
```

## Scaling Considerations

### Single Instance
- Suitable for: Testing, small deployments, low traffic
- Database: SQLite (local)
- Cache: In-memory
- Rate Limiting: Per-IP

### Multi-Instance
For production with multiple instances:

1. **Shared Database** (PostgreSQL recommended)
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=db-server;Database=web3db;Username=user;Password=pass"
   }
   ```

2. **Distributed Cache** (Redis recommended)
   ```csharp
   services.AddStackExchangeRedisCache(options => {
     options.Configuration = "redis-server:6379";
   });
   ```

3. **Distributed Rate Limiting**
   ```csharp
   services.AddDistributedRateLimiting(options => {
     options.RedisConnection = "redis-server:6379";
   });
   ```

4. **Load Balancer** (nginx recommended)
   ```nginx
   upstream web3_api {
     server api1:5000;
     server api2:5000;
     server api3:5000;
   }
   
   server {
     listen 80;
     location / {
       proxy_pass http://web3_api;
       proxy_set_header X-API-Key $http_x_api_key;
     }
   }
   ```

## Troubleshooting

### RPC Connection Issues

```bash
# Test RPC endpoint
curl -X POST https://your-rpc-endpoint \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","method":"eth_blockNumber","params":[],"id":1}'

# Check logs
docker logs web3-dashboard-api | grep -i "failed to get"
```

### Rate Limiting Issues

```bash
# Check rate limiter state
# View logs for rate limiting events
docker logs web3-dashboard-api | grep -i "rate limit"

# Adjust configuration in appsettings.json
# Restart application
```

### Authentication Failures

```bash
# Verify API key is correct
echo "X-API-Key: your-key" | curl -H @- http://localhost:5000/api/health

# Check configuration
grep -A5 "ApiKeys" appsettings.Production.json
```

## Security Best Practices

1. **Secrets Management**
   - Use Azure Key Vault, AWS Secrets Manager, or HashiCorp Vault
   - Never commit secrets to source control
   - Rotate API keys regularly

2. **HTTPS/TLS**
   - Always use HTTPS in production
   - Use Let's Encrypt for certificates (free)
   - Enable HSTS header

3. **Network Security**
   - Use VPC/private networks
   - Implement WAF (Web Application Firewall)
   - Use VPN for administrative access

4. **Monitoring**
   - Log all authentication failures
   - Alert on circuit breaker opens
   - Monitor rate limit violations

5. **Data Protection**
   - Encrypt database at rest
   - Use TLS for RPC connections
   - Implement audit logging

## Performance Optimization

1. **Caching**
   - Increase TTL for stable data (gas price, token details)
   - Use CDN for static content
   - Implement distributed cache

2. **Database**
   - Add indexes on frequently queried columns
   - Implement query pagination
   - Use connection pooling

3. **RPC**
   - Use a high-performance RPC provider
   - Implement query batching
   - Consider RPC load balancing

## Support & Documentation

- **README.md**: Quick start guide
- **CHANGELOG.md**: Version history and breaking changes
- **API Documentation**: Available at `/swagger` when running
- **Code Comments**: Inline documentation throughout codebase

## Rollback Procedure

If issues occur after deployment:

```bash
# 1. Stop current version
docker stop web3-dashboard-api

# 2. Restore previous database backup
sqlite3 /backups/web3-YYYYMMDD.db ".restore /path/to/Web3Dashboard.db"

# 3. Start previous version
docker run -d ... web3-dashboard-api:1.0

# 4. Verify
curl -H "X-API-Key: key" http://localhost:5000/api/health
```

---

**Last Updated**: 2024-02-19
**Version**: 2.0
**Maintainer**: Your Team
