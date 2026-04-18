// ============================================================
// TransactionDataService — loads and caches CSV data
// ============================================================
// FIX #2: Now also loads transactions_auth_codes.csv
//
// WHY A SERVICE?
//   We load the CSV once at startup (singleton) and serve it
//   from memory. This simulates a database query result cache.
//   In production, this would be replaced by a PostgreSQL or
//   TimescaleDB query.
//
// SQL EQUIVALENT:
//   SELECT timestamp, status, SUM(count) AS count
//   FROM transactions
//   WHERE timestamp >= NOW() - INTERVAL '24 hours'
//   GROUP BY timestamp, status
//   ORDER BY timestamp ASC;
// ============================================================

using CloudWalkMonitoring.Models;

namespace CloudWalkMonitoring.Services;

public class TransactionDataService
{
    private readonly List<TransactionRecord> _transactions;
    private readonly List<AuthCodeRecord> _authCodes;
    private readonly List<CheckoutRow> _checkout1;
    private readonly List<CheckoutRow> _checkout2;

    public TransactionDataService(IWebHostEnvironment env)
    {
        // Resolve data directory relative to the project root
        var basePath = Path.GetFullPath(Path.Combine(env.ContentRootPath, "..", "data"));

        _transactions = LoadTransactions(Path.Combine(basePath, "transactions.csv"));
        _authCodes    = LoadAuthCodes(Path.Combine(basePath, "transactions_auth_codes.csv"));
        _checkout1    = LoadCheckout(Path.Combine(basePath, "checkout_1.csv"));
        _checkout2    = LoadCheckout(Path.Combine(basePath, "checkout_2.csv"));
    }

    public List<TransactionRecord> GetTransactions(string? status, int? limit)
    {
        var q = _transactions.AsEnumerable();
        if (!string.IsNullOrEmpty(status))
            q = q.Where(t => t.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
        if (limit.HasValue)
            q = q.Take(limit.Value);
        return q.ToList();
    }

    /// <summary>
    /// FIX #2: Auth code data — tells us WHY a transaction was denied
    /// </summary>
    public List<AuthCodeRecord> GetAuthCodes(string? authCode, int? limit)
    {
        var q = _authCodes.AsEnumerable();
        if (!string.IsNullOrEmpty(authCode))
            q = q.Where(a => a.AuthCode == authCode);
        if (limit.HasValue)
            q = q.Take(limit.Value);
        return q.ToList();
    }

    public List<CheckoutRow> GetCheckoutData(int dataset) =>
        dataset == 2 ? _checkout2 : _checkout1;

    // ── Parsers ───────────────────────────────────────────────
    private static List<TransactionRecord> LoadTransactions(string path)
    {
        if (!File.Exists(path)) return [];
        var result = new List<TransactionRecord>();
        foreach (var line in File.ReadLines(path).Skip(1))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;
            var parts = trimmed.Split(',');
            if (parts.Length < 3) continue;
            if (!DateTime.TryParse(parts[0].Trim(), out var ts)) continue;
            if (!int.TryParse(parts[2].Trim(), out var count)) continue;
            result.Add(new TransactionRecord
            {
                Timestamp = ts,
                Status = parts[1].Trim().ToLower(),
                Count = count
            });
        }
        return result;
    }

    /// <summary>
    /// FIX #2: Parse transactions_auth_codes.csv
    /// Format: timestamp,auth_code,count
    /// Auth codes: 00=Approved, 51=Insufficient Funds, 59=Suspected Fraud
    /// </summary>
    private static List<AuthCodeRecord> LoadAuthCodes(string path)
    {
        if (!File.Exists(path)) return [];
        var result = new List<AuthCodeRecord>();
        foreach (var line in File.ReadLines(path).Skip(1))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;
            var parts = trimmed.Split(',');
            if (parts.Length < 3) continue;
            if (!DateTime.TryParse(parts[0].Trim(), out var ts)) continue;
            if (!int.TryParse(parts[2].Trim(), out var count)) continue;
            result.Add(new AuthCodeRecord
            {
                Timestamp = ts,
                AuthCode = parts[1].Trim(),
                Count = count
            });
        }
        return result;
    }

    private static List<CheckoutRow> LoadCheckout(string path)
    {
        if (!File.Exists(path)) return [];
        var result = new List<CheckoutRow>();
        foreach (var line in File.ReadLines(path).Skip(1))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;
            var p = trimmed.Split(',');
            if (p.Length < 6) continue;
            result.Add(new CheckoutRow
            {
                Time = p[0].Trim(),
                Today = double.TryParse(p[1], System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var v1) ? v1 : 0,
                Yesterday = double.TryParse(p[2], System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var v2) ? v2 : 0,
                SameDayLastWeek = double.TryParse(p[3], System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var v3) ? v3 : 0,
                AvgLastWeek = double.TryParse(p[4], System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var v4) ? v4 : 0,
                AvgLastMonth = double.TryParse(p[5], System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var v5) ? v5 : 0
            });
        }
        return result;
    }
}
