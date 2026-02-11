using DataTransferLib.DataAccess;
using DataTransferLib.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataTransferLib.Services
{

    // Bulk Insert ile hızlı veri aktarımı
    //Temp Table (#) kullanımı
    // MERGE ile INSERT/UPDATE/DELETE senkronizasyonu

    public class DataCopyService
    {
        private readonly DbHelper _sourceDb;
        private readonly DbHelper _targetDb;

        public DataCopyService(DbHelper sourceDb, DbHelper targetDb)
        {
            _sourceDb = sourceDb;
            _targetDb = targetDb;
        }

    
        // Ana kopyalama metodu 

        public void CopyData()
        {
            using (var sourceConn = _sourceDb.GetConnection())
            using (var targetConn = _targetDb.GetConnection())
            {
                sourceConn.Open();
                targetConn.Open();

                using (var tran = targetConn.BeginTransaction())
                {
                    try
                    {
                        //  Source'tan tüm veriyi oku
                        var employees = ReadSourceData(sourceConn);

                        //  Temp table oluştur
                        CreateTempTable(targetConn, tran);

                        //  Bulk Insert ile temp table'a veri aktar
                        BulkInsertToTemp(employees, targetConn, tran);

                        // MERGE ile senkronizasyon (INSERT/UPDATE/DELETE)
                        int affectedRows = MergeData(targetConn, tran);

                        // ADIM 5: Transaction commit
                        tran.Commit();

                        // Log için affected rows sayısını döndürebilirsin
                        Console.WriteLine($"MERGE tamamlandı. Etkilenen kayıt sayısı: {affectedRows}");
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        throw new Exception("CopyData hatası: " + ex.Message, ex);
                    }
                }
            }
        }

 
    // Source tablosundan tüm veriyi oku
  
        private List<EmployeeModel> ReadSourceData(SqlConnection sourceConn)
        {
            var employees = new List<EmployeeModel>();

            var selectCmd = new SqlCommand(@"
                SELECT 
                    TCKimlikNo,
                    FirstName,
                    LastName,
                    Email,
                    Salary,
                    BirthDate,
                    IsActive,
                    CreatedAt 
                FROM Source_Employees
            ", sourceConn);

            using (var reader = selectCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    employees.Add(new EmployeeModel
                    {
                        TCKimlikNo = reader.GetString(0),
                        FirstName = reader.GetString(1),
                        LastName = reader.GetString(2),
                        Email = reader.IsDBNull(3) ? null : reader.GetString(3),
                        Salary = reader.GetDecimal(4),
                        BirthDate = reader.GetDateTime(5),
                        IsActive = reader.GetBoolean(6),
                        CreatedAt = reader.GetDateTime(7)
                    });
                }
            }

            return employees;
        }

       
        // Temp table oluştur (TEK DİYEZ # - Local Temp Table)
    
        private void CreateTempTable(SqlConnection targetConn, SqlTransaction tran)
        {
            var createTempTableCmd = new SqlCommand(@"
                CREATE TABLE #Target_Employees_TEMP (
                    TCKimlikNo NVARCHAR(11) PRIMARY KEY,
                    FirstName NVARCHAR(50) NOT NULL,
                    LastName NVARCHAR(50) NOT NULL,
                    Email NVARCHAR(150),
                    Salary DECIMAL(18,2) NOT NULL,
                    BirthDate DATETIME NOT NULL,
                    IsActive BIT NOT NULL,
                    CreatedAt DATETIME NOT NULL
                )
            ", targetConn, tran);

            createTempTableCmd.ExecuteNonQuery();
        }


        //SqlBulkCopy ile temp table'a toplu veri aktar

        private void BulkInsertToTemp(List<EmployeeModel> employees, SqlConnection targetConn, SqlTransaction tran)
        {
            // List<EmployeeModel> → DataTable dönüşümü
            var dataTable = ConvertToDataTable(employees);

            // SqlBulkCopy yapılandırması
            using (var bulkCopy = new SqlBulkCopy(targetConn, SqlBulkCopyOptions.Default, tran))
            {
                bulkCopy.DestinationTableName = "#Target_Employees_TEMP"; // Temp table adı
                bulkCopy.BatchSize = 1000; // 1000'er kayıt gönder (performans ayarı)
                bulkCopy.BulkCopyTimeout = 300; // 5 dakika timeout

                // Kolon eşleştirmeleri
                bulkCopy.ColumnMappings.Add("TCKimlikNo", "TCKimlikNo");
                bulkCopy.ColumnMappings.Add("FirstName", "FirstName");
                bulkCopy.ColumnMappings.Add("LastName", "LastName");
                bulkCopy.ColumnMappings.Add("Email", "Email");
                bulkCopy.ColumnMappings.Add("Salary", "Salary");
                bulkCopy.ColumnMappings.Add("BirthDate", "BirthDate");
                bulkCopy.ColumnMappings.Add("IsActive", "IsActive");
                bulkCopy.ColumnMappings.Add("CreatedAt", "CreatedAt");

                // Toplu insert işlemi
                bulkCopy.WriteToServer(dataTable);
            }
        }

 
        // List<EmployeeModel> → DataTable dönüştürücü

        private DataTable ConvertToDataTable(List<EmployeeModel> employees)
        {
            var dt = new DataTable();

            // Kolonları tanımla
            dt.Columns.Add("TCKimlikNo", typeof(string));
            dt.Columns.Add("FirstName", typeof(string));
            dt.Columns.Add("LastName", typeof(string));
            dt.Columns.Add("Email", typeof(string));
            dt.Columns.Add("Salary", typeof(decimal));
            dt.Columns.Add("BirthDate", typeof(DateTime));
            dt.Columns.Add("IsActive", typeof(bool));
            dt.Columns.Add("CreatedAt", typeof(DateTime));

            // Satırları ekle
            foreach (var emp in employees)
            {
                dt.Rows.Add(
                    emp.TCKimlikNo,
                    emp.FirstName,
                    emp.LastName,
                    emp.Email ?? (object)DBNull.Value,
                    emp.Salary,
                    emp.BirthDate,
                    emp.IsActive,
                    emp.CreatedAt
                );
            }

            return dt;
        }


        // MERGE komutu 
        //Temp'te var, Target'ta yok → INSERT
        //Temp'te var, Target'ta var ama farklı → UPDATE
        //Target'ta var, Temp'te yok → DELETE

        private int MergeData(SqlConnection targetConn, SqlTransaction tran)
        {
            var mergeCmd = new SqlCommand(@"
                MERGE Target_Employees AS target
                USING #Target_Employees_TEMP AS temp
                ON (target.TCKimlikNo = temp.TCKimlikNo)

                -- Target'ta var, Temp'te var AMA farklı → UPDATE
                WHEN MATCHED AND (
                    target.FirstName <> temp.FirstName OR
                    target.LastName <> temp.LastName OR
                    ISNULL(target.Email, '') <> ISNULL(temp.Email, '') OR
                    target.Salary <> temp.Salary OR
                    target.BirthDate <> temp.BirthDate OR
                    target.IsActive <> temp.IsActive OR
                    target.CreatedAt <> temp.CreatedAt
                ) THEN
                    UPDATE SET
                        target.FirstName = temp.FirstName,
                        target.LastName = temp.LastName,
                        target.Email = temp.Email,
                        target.Salary = temp.Salary,
                        target.BirthDate = temp.BirthDate,
                        target.IsActive = temp.IsActive,
                        target.CreatedAt = temp.CreatedAt

                -- Target'ta yok, Temp'te var → INSERT
                WHEN NOT MATCHED BY TARGET THEN
                    INSERT (TCKimlikNo, FirstName, LastName, Email, Salary, BirthDate, IsActive, CreatedAt)
                    VALUES (temp.TCKimlikNo, temp.FirstName, temp.LastName, temp.Email, temp.Salary, temp.BirthDate, temp.IsActive, temp.CreatedAt)

                -- Target'ta var, Temp'te yok → DELETE (Source'tan silinmiş demektir)
                WHEN NOT MATCHED BY SOURCE THEN
                    DELETE;

            ", targetConn, tran);

            // Etkilenen kayıt sayısını döndür
            return mergeCmd.ExecuteNonQuery();
        }
    }
}





