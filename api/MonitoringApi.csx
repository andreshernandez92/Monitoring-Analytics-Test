/*
 * CloudWalk Monitoring Analytics - C# REST API
 * 
 * This is a standalone C# HTTP server that implements:
 * - POST /api/analyze   → ingests transaction data and returns anomaly analysis
 * - GET  /api/data      → returns all transactions for the dashboard
 * - GET  /api/alerts    → returns current active alerts
 * - GET  /api/health    → health check
 *
 * Algorithm: Rolling Z-Score anomaly detection (statistical rule-based)
 *
 * Run: dotnet-script MonitoringApi.csx
 * OR:  Compile as .NET 8 console app (see Program.cs)
 */

// This file documents the C# logic.
// See /api/Program.cs for the full runnable ASP.NET Core implementation.
