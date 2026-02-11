// ServiceLog modelini kullanacağımız için import
using DataTransferWinService.Models;

// Temel .NET sınıfları
using System;

// Dosya ve klasör işlemleri için
using System.IO;

// Çalışan assembly bilgisine erişmek için
using System.Reflection;

namespace DataTransferWinService.Data
{
    // Logger static — yani new’lenmeden direkt çağrılır → Logger.Log(...)
    public static class Logger
    {
        // Log yazan metod — dışarıdan ServiceLog nesnesi alır
        public static void Log(ServiceLog log)
        {
            // Çalışan exe’nin assembly bilgisini al
            var asm = Assembly.GetExecutingAssembly();

            // Exe’nin bulunduğu klasör yolunu al
            var dir = Path.GetDirectoryName(asm.Location);

            // Exe klasörü altında "logs" klasör yolu oluştur
            var logDir = Path.Combine(dir, "logs");

            // Eğer yoksa logs klasörünü oluştur (varsa hata vermez)
            Directory.CreateDirectory(logDir);

            // Gün bazlı log dosya adı üret → log_20260209.txt gibi
            var file = Path.Combine(
                logDir,
                "log_" + DateTime.Now.ToString("yyyyMMdd") + ".txt"
            );

            // Dosyayı append modda aç → varsa sonuna ekler
            using (var w = File.AppendText(file))
            {
                // Ayraç çizgisi yaz
                w.WriteLine("-----");

                // Log zamanı yaz
                w.WriteLine(DateTime.Now);

                // Eğer exception varsa hata logu yaz
                if (log.Ex != null)
                    w.WriteLine(log.MethodName + " ERROR: " + log.Ex);

                // Normal durum logu yaz
                else
                    w.WriteLine(log.MethodName + " : " + log.Description);
            } // using bitince dosya otomatik kapanır
        }
    }
}
