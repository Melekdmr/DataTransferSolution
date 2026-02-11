// ServiceSetting modelini kullanacağımız için import
using DataTransferWinService.Models;

// SQL Server bağlantısı için ADO.NET sınıfları
using System.Data.SqlClient;

namespace DataTransferWinService.Data
{
    // DB’den servis ayarlarını okuyan repository sınıfı
    public class ServiceSettingsRepository
    {
        // Connection string’i tutan private readonly alan
        private readonly string _conn;

        // Constructor — dışarıdan connection string alır
        public ServiceSettingsRepository(string conn)
        {
            _conn = conn; // gelen connection string saklanır
        }

        // Servis ayarlarını DB’den okuyup dönen metod
        public ServiceSetting GetSettings()
        {
            // Döndürülecek model nesnesi oluşturulur
            var result = new ServiceSetting();

            // SqlConnection oluştur — using → iş bitince otomatik dispose
            using (var conn = new SqlConnection(_conn))

            // SQL komutu oluştur — TOP 1 → ilk kaydı al
            using (var cmd = new SqlCommand(
                "SELECT TOP 1 IntervalMinutes, IsActive FROM ServiceSettings",
                conn))
            {
                // DB bağlantısını aç
                conn.Open();

                // Komutu çalıştır → DataReader döner
                using (var r = cmd.ExecuteReader())
                {
                    // Kayıt var mı kontrol et
                    if (r.Read())
                    {
                        // IntervalMinutes kolonunu int olarak modele ata
                        result.IntervalMinutes = (int)r["IntervalMinutes"];

                        // IsActive kolonunu bool olarak modele ata
                        result.IsActive = (bool)r["IsActive"];
                    }
                }
            }

            // Doldurulmuş ayar nesnesini geri döndür
            return result;
        }
    }
}
