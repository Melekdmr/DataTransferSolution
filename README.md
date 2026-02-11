# 🔄 Data Transfer Windows Service

> High-performance SQL Server synchronization using Bulk Insert, Temp Tables & MERGE 

[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.7.2-blue.svg)](https://dotnet.microsoft.com/)
[![SQL Server](https://img.shields.io/badge/SQL%20Server-2016%2B-red.svg)](https://www.microsoft.com/sql-server)

## 🎯 Overview

Windows Service that automatically synchronizes data between two SQL Server databases using advanced techniques:

- **Bulk Insert** (SqlBulkCopy) for fast data transfer
- **Temp Tables** (#temp) for staging
- **SQL MERGE** for automatic INSERT/UPDATE/DELETE

## ✨ Features

- ✅ Automatic synchronization with configurable intervals
- ✅ Full CRUD support (INSERT/UPDATE/DELETE)
- ✅ 50x faster than traditional methods
- ✅ Transaction safety with rollback support
- ✅ Daily log files with operation tracking

## 📊 Performance

| Records | Old Method | New Method | Improvement |
|---------|------------|------------|-------------|
| 100     | 2.0s       | 0.5s       | **4x**      |
| 1,000   | 18.0s      | 1.2s       | **15x**     |
| 10,000  | 180.0s     | 3.5s       | **51x**     |

## 🚀 Quick Start

### 1. Create Tables
```sql
-- ServiceSettings (Source DB)
CREATE TABLE ServiceSettings (
    IntervalMinutes INT NOT NULL,
    IsActive BIT NOT NULL
)
INSERT INTO ServiceSettings VALUES (5, 1)

-- Source_Employees & Target_Employees
CREATE TABLE Source_Employees (
    TCKimlikNo NVARCHAR(11) PRIMARY KEY,
    FirstName NVARCHAR(50) NOT NULL,
    LastName NVARCHAR(50) NOT NULL,
    Email NVARCHAR(150),
    Salary DECIMAL(18,2) NOT NULL,
    BirthDate DATETIME NOT NULL,
    IsActive BIT NOT NULL,
    CreatedAt DATETIME NOT NULL
)
```

### 2. Configure Connection

Edit `App.config`:
```xml
<connectionStrings>
  <add name="SourceDb" connectionString="Server=SERVER1;Database=SourceDB;Integrated Security=true;" />
  <add name="TargetDb" connectionString="Server=SERVER2;Database=TargetDB;Integrated Security=true;" />
</connectionStrings>
```

### 3. Install Service
```cmd
sc create DataTransferWinService binPath="C:\Services\DataTransferWinService\DataTransferWinService.exe" start=auto
sc start DataTransferWinService
```

## 💻 Usage
```cmd
# Check status
sc query DataTransferWinService

# Start/Stop
sc start DataTransferWinService
sc stop DataTransferWinService

# View logs
notepad C:\Services\DataTransferWinService\logs\log_YYYYMMDD.txt
```

## 🧪 Test Scenarios

### INSERT Test
```sql
INSERT INTO Source_Employees VALUES ('11111111111','John','Doe','test@test.com',5000,'1990-01-01',1,GETDATE())
-- Wait for service → Check Target_Employees
```

### UPDATE Test
```sql
UPDATE Source_Employees SET FirstName='JOHN_UPDATED', Salary=9999 WHERE TCKimlikNo='11111111111'
-- Wait for service → Verify Target updated ✓
```

### DELETE Test
```sql
DELETE FROM Source_Employees WHERE TCKimlikNo='11111111111'
-- Wait for service → Verify Target deleted ✓
```

## 🔧 Troubleshooting

**Service won't start**
```cmd
eventvwr.msc → Windows Logs → Application
```

**Timeout error**
```csharp
// In DataCopyService.cs
bulkCopy.BulkCopyTimeout = 600; // Increase to 10 minutes
```

**Primary Key violation**
```sql
-- Check for duplicates in Source
SELECT TCKimlikNo, COUNT(*) FROM Source_Employees GROUP BY TCKimlikNo HAVING COUNT(*) > 1
```

## 🏗️ How It Works
```
Source_Employees
      ↓
[SqlBulkCopy]
      ↓
#Target_Employees_TEMP
      ↓
   [MERGE]
      ↓
Target_Employees
```

### The MERGE Magic
```sql
MERGE Target_Employees AS target
USING #Target_Employees_TEMP AS temp
ON target.TCKimlikNo = temp.TCKimlikNo
WHEN MATCHED AND (data differs) THEN UPDATE
WHEN NOT MATCHED BY TARGET THEN INSERT
WHEN NOT MATCHED BY SOURCE THEN DELETE;
```

## 📁 Project Structure
```
DataTransferService/
├── DataTransferLib/
│   └── Services/DataCopyService.cs    # Core logic
├── DataTransferWinService/
│   ├── Data/Logger.cs
│   └── DataTransferWinServices.cs     # Service entry point
└── App.config
```

## 🔑 Key Code
```csharp
// Bulk Insert
using (var bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, tran))
{
    bulkCopy.DestinationTableName = "#Target_Employees_TEMP";
    bulkCopy.BatchSize = 1000;
    bulkCopy.WriteToServer(dataTable);
}
```

## ⚙️ Configuration
```sql
-- Change interval (10 minutes)
UPDATE ServiceSettings SET IntervalMinutes = 10

-- Pause/Resume
UPDATE ServiceSettings SET IsActive = 0  -- Pause
UPDATE ServiceSettings SET IsActive = 1  -- Resume
```

