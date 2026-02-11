using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Diagnostics.Tracing;





namespace DataTransferLib.DataAccess
{
    public class DbHelper
    {
        //veritabanı bağlantısını merkezileştirir.
        private readonly string _connectionString;

        //connection üretme işi tek yerde yapılır
        public DbHelper(string connectionString)
        {
            _connectionString = connectionString;
        }

        public SqlConnection GetConnection() //sql bağlantus nesnesi döndür - bağkantıyı getir
        {
            return new SqlConnection(_connectionString);
        }
    }
}
//Her yerde new SqlConnection yazmamak için