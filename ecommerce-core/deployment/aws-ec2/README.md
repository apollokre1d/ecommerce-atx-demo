# AWS EC2 Deployment Strategy

## 🖥️ **SIMPLE DEPLOYMENT: AWS EC2 Instances**

### **Architecture:**
```
Internet → ALB → EC2 Web Servers → SQL Server (EC2)
```

### **Components:**
- **Application Load Balancer** - Traffic distribution
- **EC2 Web Servers** - 2x t3.medium instances running .NET
- **SQL Server** - Your existing EC2 instance
- **Auto Scaling Group** - Automatic scaling based on demand

### **Benefits:**
- ✅ **Simple setup** - Traditional server deployment
- ✅ **Full control** - Direct access to servers
- ✅ **Cost predictable** - Fixed monthly costs
- ✅ **Easy debugging** - Direct server access via RDP/SSH

### **Estimated Costs:**
- **2x t3.medium Web Servers:** ~$60/month
- **Application Load Balancer:** ~$20/month
- **SQL Server EC2:** Already running
- **Total:** ~$80/month

---

## 📋 **DEPLOYMENT STEPS**

### **1. Create Web Server Instances**
```bash
# Launch 2 web server instances
aws ec2 run-instances \
  --image-id ami-0c02fb55956c7d316 \
  --count 2 \
  --instance-type t3.medium \
  --key-name your-key-pair \
  --security-group-ids sg-web-servers \
  --subnet-id subnet-12345 \
  --user-data file://web-server-userdata.sh \
  --tag-specifications 'ResourceType=instance,Tags=[{Key=Name,Value=ECommerce-Web-Server}]'
```

### **2. Install .NET Runtime**
```powershell
# PowerShell script for Windows Server
# Download and install .NET 8 Runtime
Invoke-WebRequest -Uri "https://download.microsoft.com/download/dotnet/8.0/dotnet-hosting-8.0-win.exe" -OutFile "dotnet-hosting.exe"
Start-Process -FilePath "dotnet-hosting.exe" -ArgumentList "/quiet" -Wait

# Install IIS and ASP.NET Core Module
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole, IIS-WebServer, IIS-CommonHttpFeatures, IIS-HttpErrors, IIS-HttpLogging, IIS-RequestFiltering, IIS-StaticContent, IIS-DefaultDocument, IIS-DirectoryBrowsing
```

### **3. Application Deployment**
```bash
# Build and publish application
dotnet publish -c Release -o ./publish

# Copy to web servers (using PowerShell/SCP)
scp -r ./publish/* ec2-user@web-server-1:/var/www/ecommerce/
scp -r ./publish/* ec2-user@web-server-2:/var/www/ecommerce/
```

### **4. Configure IIS/Nginx**
```xml
<!-- IIS web.config -->
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <handlers>
      <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
    </handlers>
    <aspNetCore processPath="dotnet" arguments=".\ECommerce.Web.dll" stdoutLogEnabled="false" stdoutLogFile=".\logs\stdout" />
  </system.webServer>
</configuration>
```

---

## 🔧 **CONFIGURATION**

### **Connection String:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=10.0.2.88,1433;Database=ECommerceDB;User Id=sa;Password=YourPassword;TrustServerCertificate=true;"
  }
}
```

### **Load Balancer Health Check:**
- **Path:** `/health`
- **Port:** `80`
- **Protocol:** `HTTP`
- **Interval:** `30 seconds`

---

## 🚀 **DEPLOYMENT AUTOMATION**

### **PowerShell Deployment Script:**
```powershell
# deploy.ps1
param(
    [string]$Environment = "Production"
)

# Build application
dotnet publish -c Release -o ./publish

# Stop IIS application pool
Stop-WebAppPool -Name "ECommerceApp"

# Copy files
Copy-Item -Path "./publish/*" -Destination "C:\inetpub\wwwroot\ecommerce\" -Recurse -Force

# Update configuration
(Get-Content "C:\inetpub\wwwroot\ecommerce\appsettings.json") -replace "ENVIRONMENT_PLACEHOLDER", $Environment | Set-Content "C:\inetpub\wwwroot\ecommerce\appsettings.json"

# Start IIS application pool
Start-WebAppPool -Name "ECommerceApp"

Write-Host "Deployment completed successfully!"
```

---

## 📊 **MONITORING**

### **CloudWatch Agent:**
```json
{
  "metrics": {
    "namespace": "ECommerce/Application",
    "metrics_collected": {
      "cpu": {
        "measurement": ["cpu_usage_idle", "cpu_usage_iowait", "cpu_usage_user", "cpu_usage_system"],
        "metrics_collection_interval": 60
      },
      "disk": {
        "measurement": ["used_percent"],
        "metrics_collection_interval": 60,
        "resources": ["*"]
      },
      "mem": {
        "measurement": ["mem_used_percent"],
        "metrics_collection_interval": 60
      }
    }
  },
  "logs": {
    "logs_collected": {
      "files": {
        "collect_list": [
          {
            "file_path": "C:\\inetpub\\logs\\LogFiles\\W3SVC1\\*.log",
            "log_group_name": "/aws/ec2/ecommerce/iis",
            "log_stream_name": "{instance_id}/iis"
          }
        ]
      }
    }
  }
}
```