using System;
using System.ServiceProcess;
using DataTransferLib.DataAccess;
using DataTransferLib.Services;
using DataTransferWinService.Data;
using DataTransferWinService.Models;
using System.Configuration;
using System.Timers;

namespace DataTransferWinServices
{


    // -Bulk Insert, Temp Table, MERGE 


    public partial class DataTransferWinServices : ServiceBase
    {
        private Timer _timer;
        private bool _isRunning = false;

        private readonly string _sourceConnStr =
            ConfigurationManager.ConnectionStrings["SourceDb"].ConnectionString;

        private readonly string _targetConnStr =
            ConfigurationManager.ConnectionStrings["TargetDb"].ConnectionString;

        public DataTransferWinServices()
        {
            InitializeComponent();
            this.ServiceName = "DataTransferWinService";
        }

        protected override void OnStart(string[] args)
        {
            Logger.Log(new ServiceLog
            {
                MethodName = "OnStart",
                Description = "Servis başladı (Bulk+Temp+Merge)"
            });

            StartTimerFromDb();
        }

        private void StartTimerFromDb()
        {
            try
            {
                var repo = new ServiceSettingsRepository(_sourceConnStr);
                var settings = repo.GetSettings();

                int intervalMs = settings.IntervalMinutes * 60000;

                _timer = new Timer(intervalMs);
                _timer.Elapsed += TimerElapsed;
                _timer.AutoReset = true;
                _timer.Start();

                Logger.Log(new ServiceLog
                {
                    MethodName = "StartTimerFromDb",
                    Description = $"Timer ayarlandı: {settings.IntervalMinutes} dakika"
                });
            }
            catch (Exception ex)
            {
                Logger.Log(new ServiceLog
                {
                    MethodName = "StartTimerFromDb",
                    Ex = ex
                });
            }
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            CheckAndRun();
        }

        private void CheckAndRun()
        {
            // Zaten çalışıyorsa tekrar başlatma
            if (_isRunning)
            {
                Logger.Log(new ServiceLog
                {
                    MethodName = "CheckAndRun",
                    Description = "Önceki işlem hala devam ediyor, atlanıyor"
                });
                return;
            }

            try
            {
                var repo = new ServiceSettingsRepository(_sourceConnStr);
                var settings = repo.GetSettings();

                if (!settings.IsActive)
                {
                    Logger.Log(new ServiceLog
                    {
                        MethodName = "CheckAndRun",
                        Description = "Servis pasif durumda (IsActive=false)"
                    });
                    return;
                }

                RunCopy();
            }
            catch (Exception ex)
            {
                Logger.Log(new ServiceLog
                {
                    MethodName = "CheckAndRun",
                    Ex = ex
                });
            }
        }

        private void RunCopy()
        {
            _isRunning = true;
            var startTime = DateTime.Now;

            try
            {
                Logger.Log(new ServiceLog
                {
                    MethodName = "RunCopy",
                    Description = "Kopyalama işlemi başladı (Bulk+Temp+Merge)"
                });

                var sourceDb = new DbHelper(_sourceConnStr);
                var targetDb = new DbHelper(_targetConnStr);
                var svc = new DataCopyService(sourceDb, targetDb);

                // Yeni CopyData metodu çalıştırılır
                svc.CopyData();

                var duration = DateTime.Now.Subtract(startTime);

                Logger.Log(new ServiceLog
                {
                    MethodName = "RunCopy",
                    Description = $"Kopyalama başarıyla tamamlandı. Süre: {duration.TotalSeconds:F2} saniye"
                });
            }
            catch (Exception ex)
            {
                Logger.Log(new ServiceLog
                {
                    MethodName = "RunCopy",
                    Ex = ex
                });
            }
            finally
            {
                _isRunning = false;
            }
        }

        protected override void OnStop()
        {
            _timer?.Stop();
            _timer?.Dispose();

            Logger.Log(new ServiceLog
            {
                MethodName = "OnStop",
                Description = "Servis durduruldu"
            });
        }
    }
}