// ============================================================
// Models for CloudWalk Monitoring API
// ============================================================
namespace CloudWalkMonitoring.Models;

/// <summary>
/// A single minute-level transaction record from the CSV.
/// timestamp | status | count
/// </summary>
public class TransactionRecord
{
    public DateTime Timestamp { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}

/// <summary>
/// FIX #2: Auth code record from transactions_auth_codes.csv
/// Auth codes are ISO 8583 response codes that tell us WHY a transaction
/// was approved/denied — not just what happened, but the root cause.
///
///   00 = Approved
///   51 = Insufficient Funds
///   59 = Suspected Fraud
/// </summary>
public class AuthCodeRecord
{
    public DateTime Timestamp { get; set; }
    public string AuthCode { get; set; } = string.Empty;
    public int Count { get; set; }
}

/// <summary>
/// Alert fired when an anomaly is detected.
/// Severity: WARNING (1.5–2.5σ), CRITICAL (>2.5σ)
/// </summary>
public class Alert
{
    public DateTime Timestamp { get; set; }
    public string Status { get; set; } = string.Empty;
    public int ObservedCount { get; set; }
    public double RollingMean { get; set; }
    public double RollingStdDev { get; set; }
    public double ZScore { get; set; }
    public string Severity { get; set; } = string.Empty;   // "WARNING" | "CRITICAL"
    public string Type { get; set; } = string.Empty;        // "SPIKE" | "DROP"
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// POST /api/analyze request body
/// </summary>
public class AnalyzeRequest
{
    public List<TransactionRecord> Records { get; set; } = [];
}

/// <summary>
/// One row from the checkout CSV files
/// </summary>
public class CheckoutRow
{
    public string Time { get; set; } = string.Empty;
    public double Today { get; set; }
    public double Yesterday { get; set; }
    public double SameDayLastWeek { get; set; }
    public double AvgLastWeek { get; set; }
    public double AvgLastMonth { get; set; }
}
