using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using CloudWalkMonitoring.Models;
using CloudWalkMonitoring.Services;

namespace MonitoringTests
{
    public class AnomalyDetectionTests
    {
        private readonly AnomalyDetectionService _sut; 

        public AnomalyDetectionTests()
        {
            _sut = new AnomalyDetectionService();
        }

        [Fact]
        public void DetectAll_FlatSeries_ProducesNoAlerts()
        {
            var records = new List<TransactionRecord>();
            var start = DateTime.UtcNow;
            
            // Absolutely zero variance -> stdDev = 0 -> gracefully skips
            for (int i = 0; i < 50; i++)
            {
                records.Add(new TransactionRecord
                {
                    Timestamp = start.AddMinutes(i),
                    Status = "approved",
                    Count = 100 
                });
            }

            var alerts = _sut.DetectAll(records);
            Assert.Empty(alerts); 
        }

        [Fact]
        public void DetectAll_SuddenSpike_TriggersCriticalAlert()
        {
            var records = new List<TransactionRecord>();
            var start = DateTime.UtcNow;
            
            int[] baseline = { 50, 51, 49, 52, 48, 50, 50, 51, 49, 50, 50, 51, 49, 52, 48, 50, 50, 51, 49, 50, 50, 51, 49, 52, 48, 50, 50, 51, 49, 50 };

            for (int i = 0; i < 30; i++)
            {
                records.Add(new TransactionRecord
                {
                    Timestamp = start.AddMinutes(i),
                    Status = "denied",
                    Count = baseline[i]
                });
            }

            // Spike at minute 30
            records.Add(new TransactionRecord
            {
                Timestamp = start.AddMinutes(30),
                Status = "denied",
                Count = 500
            });

            var alerts = _sut.DetectAll(records).Where(a => a.Status != "failure_rate").ToList();

            Assert.NotEmpty(alerts);
            var spikeAlert = alerts.First(a => a.Status == "denied" && a.ObservedCount == 500);
            
            Assert.Equal("SPIKE", spikeAlert.Type);
            Assert.Equal("CRITICAL", spikeAlert.Severity);
            Assert.True(spikeAlert.ZScore > 2.5);
        }

        [Fact]
        public void DetectAll_FailureRateExceedsThreshold_TriggersRuleAlert()
        {
            var records = new List<TransactionRecord>
            {
                new() { Timestamp = DateTime.UnixEpoch, Status = "approved", Count = 80 },
                new() { Timestamp = DateTime.UnixEpoch, Status = "failed", Count = 20 }
            };

            var alerts = _sut.DetectAll(records);

            var rateAlert = alerts.FirstOrDefault(a => a.Status == "failure_rate");
            Assert.NotNull(rateAlert);
            Assert.Equal(0.20, rateAlert.ZScore, 3); 
            Assert.Equal("WARNING", rateAlert.Severity); 
        }
        
        [Fact]
        public void DetectAll_ApprovedDrop_TriggersCriticalAlert()
        {
            var records = new List<TransactionRecord>();
            var start = DateTime.UtcNow;
            
            // Baseline 200, 205, 195, 200...
            int[] baseline = { 200, 205, 195, 202, 198, 200, 200, 205, 195, 200, 200, 205, 195, 202, 198, 200, 200, 205, 195, 200, 200, 205, 195, 202, 198, 200, 200, 205, 195, 200 };
            
            for (int i = 0; i < 30; i++)
            {
                records.Add(new TransactionRecord
                {
                    Timestamp = start.AddMinutes(i),
                    Status = "approved",
                    Count = baseline[i]
                });
            }

            // Sudden drop at minute 30
            records.Add(new TransactionRecord
            {
                Timestamp = start.AddMinutes(30),
                Status = "approved",
                Count = 10
            });

            var alerts = _sut.DetectAll(records);

            Assert.NotEmpty(alerts);
            // Ignore any minor noise alerts, find the drop alert
            var dropAlert = alerts.First(a => a.Type == "DROP" && a.ObservedCount == 10);
            
            Assert.Equal("approved", dropAlert.Status);
            Assert.True(dropAlert.ZScore < -2.0); 
        }
    }
}
