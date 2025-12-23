using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sitas.Edge.Core;
using Sitas.Edge.EdgePlcDriver;
using Sitas.Edge.EdgePlcDriver.Messages;
using Sitas.Edge.EdgePlcDriver.DataTypes;
using Microsoft.Extensions.Logging;

namespace PlcWinFormsApp;

public partial class MainForm : Form
{
    private Label? _lotNumberLabel;
    private Label? _statusLabel;
    private ISitasEdge? _sitasEdge;
    private IEdgePlcDriver? _plcConnection;
    private System.Threading.Timer? _readTimer;
    private readonly ILoggerFactory _loggerFactory;

    public MainForm()
    {
        InitializeComponent();
        _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        InitializeSitasEdge();
    }

    private void InitializeComponent()
    {
        this.Text = "PLC Lot Number Reader";
        this.Size = new System.Drawing.Size(600, 200);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;

        // Status Label
        _statusLabel = new Label
        {
            Text = "Connecting to PLC...",
            Location = new System.Drawing.Point(20, 20),
            Size = new System.Drawing.Size(550, 30),
            Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular)
        };
        this.Controls.Add(_statusLabel);

        // Lot Number Label
        _lotNumberLabel = new Label
        {
            Text = "Lot Number: --",
            Location = new System.Drawing.Point(20, 60),
            Size = new System.Drawing.Size(550, 50),
            Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold),
            ForeColor = System.Drawing.Color.DarkBlue
        };
        this.Controls.Add(_lotNumberLabel);
    }

    private async void InitializeSitasEdge()
    {
        try
        {
            // PLC Configuration - same as ConduitPlcDemo
            const string plcIp = "192.168.8.55";
            const int slot = 0;

            // Create logger
            var logger = _loggerFactory.CreateLogger<MainForm>();

            // Build Sitas.Edge with PLC connection
            _sitasEdge = SitasEdgeBuilder.Create()
                .AddEdgePlcDriver(plc => plc
                    .WithConnectionName("plc1")
                    .WithPlc(plcIp, cpuSlot: slot)
                    .WithDefaultPollingInterval(100)
                    .WithAutoReconnect(enabled: false, maxDelaySeconds: 30)
                    .WithLoggerFactory(_loggerFactory))
                .Build();

            // Get PLC connection
            _plcConnection = _sitasEdge.GetConnection<IEdgePlcDriver>();

            // Connect to PLC
            await _sitasEdge.ConnectAllAsync();
            await Task.Delay(500);

            if (!_plcConnection.IsConnected)
            {
                UpdateStatus("❌ Failed to connect to PLC");
                return;
            }

            UpdateStatus("✅ Connected to PLC");
            
            // Start reading every 5 seconds
            _readTimer = new System.Threading.Timer(async _ => await ReadLotNumber(), null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        }
        catch (Exception ex)
        {
            UpdateStatus($"❌ Error: {ex.Message}");
        }
    }

    private async Task ReadLotNumber()
    {
        if (_plcConnection == null || !_plcConnection.IsConnected)
        {
            UpdateStatus("❌ PLC not connected");
            return;
        }

        try
        {
            // Read the lot number tag
            var tagPath = "ngpSampleCurrent.pallets[0].cavities[0].lotNumber";
            var result = await _plcConnection.ReadTagAsync<LogixString>(tagPath);

            if (result != null && result.Quality == TagQuality.Good)
            {
                var lotNumber = result.Value?.Value ?? string.Empty;
                
                // Update UI on UI thread
                if (InvokeRequired)
                {
                    Invoke(new Action(() => UpdateLotNumber(lotNumber)));
                }
                else
                {
                    UpdateLotNumber(lotNumber);
                }
            }
            else
            {
                UpdateStatus($"⚠️ Tag read failed. Quality: {result?.Quality}");
            }
        }
        catch (Exception ex)
        {
            UpdateStatus($"❌ Read error: {ex.Message}");
        }
    }

    private void UpdateLotNumber(string lotNumber)
    {
        if (_lotNumberLabel != null)
        {
            _lotNumberLabel.Text = $"Lot Number: {lotNumber}";
        }
    }

    private void UpdateStatus(string status)
    {
        if (_statusLabel != null && InvokeRequired)
        {
            Invoke(new Action(() => _statusLabel.Text = status));
        }
        else if (_statusLabel != null)
        {
            _statusLabel.Text = status;
        }
    }

    protected override async void OnFormClosing(FormClosingEventArgs e)
    {
        _readTimer?.Dispose();
        
        if (_sitasEdge != null)
        {
            await _sitasEdge.DisconnectAllAsync();
            await _sitasEdge.DisposeAsync();
        }

        _loggerFactory?.Dispose();
        base.OnFormClosing(e);
    }
}

