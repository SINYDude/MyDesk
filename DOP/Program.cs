// Desk Of Paul (DOP) — Terminal Dashboard
// Terminal.Gui v2 | .NET 10

using System.Diagnostics;
using System.Net.NetworkInformation;
using Terminal.Gui;
using TAttr = Terminal.Gui.Attribute;
using DOP;

// ── Config ────────────────────────────────────────────────────────────────────

var configPath = Path.Combine(AppContext.BaseDirectory, "config.ini");
var cfg = Config.Load(configPath);

// ── HTTP client (declared early — referenced by timer closures) ───────────────

using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
DateTime lastPublicIpFetch = DateTime.MinValue;
string publicIp = "--";

// ── Accent color cycling ──────────────────────────────────────────────────────

Color[] accentCycle =
    [Color.Cyan, Color.Green, Color.Yellow, Color.Red, Color.White, Color.BrightBlue];

int accentIdx = Math.Max(0, Array.FindIndex(accentCycle, c => c == cfg.AccentColor));

Color Accent() => accentCycle[accentIdx];

ColorScheme AccentScheme() => new()
{
    Normal    = new TAttr(Accent(), cfg.Background),
    Focus     = new TAttr(cfg.Background, Accent()),
    HotNormal = new TAttr(Accent(), cfg.Background),
    HotFocus  = new TAttr(cfg.Background, Accent()),
    Disabled  = new TAttr(Color.DarkGray, cfg.Background),
};

ColorScheme GridScheme() => new()
{
    Normal    = new TAttr(cfg.GridColor, cfg.Background),
    Focus     = new TAttr(cfg.GridColor, cfg.Background),
    HotNormal = new TAttr(Accent(), cfg.Background),
    HotFocus  = new TAttr(cfg.Background, Accent()),
    Disabled  = new TAttr(Color.DarkGray, cfg.Background),
};

// ListView needs its own scheme so the selected row is visually distinct
ColorScheme ListScheme() => new()
{
    Normal    = new TAttr(Color.White, cfg.Background),
    Focus     = new TAttr(cfg.Background, Accent()),   // selected row: accent background
    HotNormal = new TAttr(Accent(), cfg.Background),
    HotFocus  = new TAttr(cfg.Background, Accent()),
    Disabled  = new TAttr(Color.DarkGray, cfg.Background),
};

// ── ASCII title ───────────────────────────────────────────────────────────────

const string AsciiTitle =
    ".·:''''''''''''''''''''''''''''''''''''''''''''''''''''''''':·.\n" +
    ": :  ____            _       ___   __   ____             _  : :\n" +
    ": : |  _ \\  ___  ___| | __  / _ \\ / _| |  _ \\ __ _ _   _| | : :\n" +
    ": : | | | |/ _ \\/ __| |/ / | | | | |_  | |_) / _` | | | | | : :\n" +
    ": : | |_| |  __/\\__ \\   <  | |_| |  _| |  __/ (_| | |_| | | : :\n" +
    ": : |____/ \\___||___/_|\\_\\  \\___/|_|   |_|   \\__,_|\\__,_|_| : :\n" +
    "'·:.........................................................:·'";

// ── Layout constants ──────────────────────────────────────────────────────────

const int TitleH    = 7;
const int LauncherW = 34;
const int ClockW    = 24;
const int SysH      = 5;

// ── Init ──────────────────────────────────────────────────────────────────────

Application.Init();
var top = new Toplevel();

// ── Title ─────────────────────────────────────────────────────────────────────

var titleLbl = new Label
{
    X      = 0,
    Y      = 0,
    Width  = Dim.Fill(),
    Height = TitleH,
    Text   = AsciiTitle,
    ColorScheme = AccentScheme(),
};
top.Add(titleLbl);

// ── Launcher ──────────────────────────────────────────────────────────────────

var launcherFrame = new FrameView
{
    Title    = "Launcher",
    X        = 0,
    Y        = TitleH,
    Width    = LauncherW,
    Height   = Dim.Fill(1),
    CanFocus = true,
    ColorScheme = GridScheme(),
};
top.Add(launcherFrame);

int selectedIdx = 0;
var launcherLabels = new List<Label>();

void BuildLauncher()
{
    launcherFrame.RemoveAll();
    launcherLabels.Clear();
    // clamp selection after config reload
    if (selectedIdx >= cfg.Links.Count) selectedIdx = 0;

    for (int i = 0; i < cfg.Links.Count; i++)
    {
        var link = cfg.Links[i];
        var lbl = new Label
        {
            X    = 0,
            Y    = i,
            Width = Dim.Fill(),
            Text  = link.IsSeparator
                ? " " + new string('─', LauncherW - 6)
                : $"[{(link.Hotkey.Length > 0 ? $"{link.Hotkey,-3}" : "   ")}] {link.Label}",
            ColorScheme = (!link.IsSeparator && i == selectedIdx)
                ? ListScheme()
                : GridScheme(),
        };
        launcherFrame.Add(lbl);
        launcherLabels.Add(lbl);
    }
}

BuildLauncher();


// ── Clock ─────────────────────────────────────────────────────────────────────

var clockFrame = new FrameView
{
    Title  = "Clock",
    X      = LauncherW, Y = TitleH,
    Width  = ClockW, Height = 3,
    ColorScheme = GridScheme(),
};
var clockLbl = new Label { X = 1, Y = 0, Width = Dim.Fill(), Text = "--:-- --" };
clockFrame.Add(clockLbl);
top.Add(clockFrame);

// ── Weather ───────────────────────────────────────────────────────────────────

var weatherFrame = new FrameView
{
    Title  = "Weather",
    X      = LauncherW + ClockW, Y = TitleH,
    Width  = Dim.Fill(), Height = 3,
    ColorScheme = GridScheme(),
};
var weatherLbl = new Label { X = 1, Y = 0, Width = Dim.Fill(), Text = "Loading..." };
weatherFrame.Add(weatherLbl);
top.Add(weatherFrame);

// ── System resources ──────────────────────────────────────────────────────────

var sysFrame = new FrameView
{
    Title  = "System",
    X      = LauncherW, Y = TitleH + 3,
    Width  = Dim.Fill(), Height = SysH,
    ColorScheme = GridScheme(),
};
var cpuLbl = new Label { X = 1, Y = 0, Width = Dim.Fill(), Text = "CPU : --%  [----------]" };
var ramLbl = new Label { X = 1, Y = 1, Width = Dim.Fill(), Text = "RAM : -- GB available" };
sysFrame.Add(cpuLbl, ramLbl);
top.Add(sysFrame);

// ── Network ───────────────────────────────────────────────────────────────────

var netFrame = new FrameView
{
    Title  = "Network",
    X      = LauncherW, Y = TitleH + 3 + SysH,
    Width  = Dim.Fill(), Height = Dim.Fill(1),
    ColorScheme = GridScheme(),
};
var netStatusLbl  = new Label { X = 1, Y = 0, Width = Dim.Fill(), Text = "Status   : Checking..." };
var netNameLbl    = new Label { X = 1, Y = 1, Width = Dim.Fill(), Text = "Network  : --" };
var netAdapterLbl = new Label { X = 1, Y = 2, Width = Dim.Fill(), Text = "Adapter  : --" };
var netLatencyLbl = new Label { X = 1, Y = 3, Width = Dim.Fill(), Text = "Latency  : --" };
var netLocalLbl   = new Label { X = 1, Y = 4, Width = Dim.Fill(), Text = "Local IP : --" };
var netPublicLbl  = new Label { X = 1, Y = 5, Width = Dim.Fill(), Text = "Public   : --" };
netFrame.Add(netStatusLbl, netNameLbl, netAdapterLbl, netLatencyLbl, netLocalLbl, netPublicLbl);
top.Add(netFrame);

// ── Status bar ────────────────────────────────────────────────────────────────

var statusBar = new Label
{
    X = 0, Y = Pos.AnchorEnd(1),
    Width = Dim.Fill(),
    Text  = " ^T:Theme  F5:Reload  Esc:Exit  F1-F12:Launch",
    ColorScheme = GridScheme(),
};
top.Add(statusBar);

// ── Theme ─────────────────────────────────────────────────────────────────────

void ApplyTheme()
{
    titleLbl.ColorScheme      = AccentScheme();
    launcherFrame.ColorScheme = GridScheme();
    clockFrame.ColorScheme    = GridScheme();
    weatherFrame.ColorScheme  = GridScheme();
    sysFrame.ColorScheme      = GridScheme();
    netFrame.ColorScheme      = GridScheme();
    statusBar.ColorScheme     = GridScheme();
    BuildLauncher();
}

// ── Key handling ──────────────────────────────────────────────────────────────

Application.KeyDown += (_, e) =>
{
    var key = e.KeyCode;

    if (key == (KeyCode.T | KeyCode.CtrlMask))
    {
        accentIdx = (accentIdx + 1) % accentCycle.Length;
        ApplyTheme();
        e.Handled = true;
        return;
    }

    if (key == KeyCode.F5)
    {
        cfg = Config.Load(configPath);
        accentIdx = Math.Max(0, Array.FindIndex(accentCycle, c => c == cfg.AccentColor));
        ApplyTheme();
        e.Handled = true;
        return;
    }

    if (key == KeyCode.Esc)
    {
        Application.RequestStop();
        e.Handled = true;
        return;
    }

    // Launcher navigation — handled globally so focus state doesn't matter
    if (key == KeyCode.CursorUp)
    {
        MoveSelection(-1);
        e.Handled = true;
        return;
    }

    if (key == KeyCode.CursorDown)
    {
        MoveSelection(1);
        e.Handled = true;
        return;
    }

    if (key == KeyCode.Enter)
    {
        ActivateSelected();
        e.Handled = true;
        return;
    }

    foreach (var link in cfg.Links)
    {
        if (link.IsSeparator || link.Path.Length == 0 || link.Hotkey.Length == 0) continue;
        if (link.Path == "EXIT") continue;
        if (Enum.TryParse<KeyCode>(link.Hotkey, ignoreCase: true, out var fkey) && key == fkey)
        {
            try { Process.Start(new ProcessStartInfo(link.Path) { UseShellExecute = true }); }
            catch { }
            e.Handled = true;
            return;
        }
    }
};

void MoveSelection(int dir)
{
    int next = selectedIdx + dir;
    while (next >= 0 && next < cfg.Links.Count)
    {
        if (!cfg.Links[next].IsSeparator)
        {
            launcherLabels[selectedIdx].ColorScheme = GridScheme();
            selectedIdx = next;
            launcherLabels[selectedIdx].ColorScheme = ListScheme();
            return;
        }
        next += dir;
    }
}

void ActivateSelected()
{
    int idx = selectedIdx;
    if (idx < 0 || idx >= cfg.Links.Count) return;
    var link = cfg.Links[idx];
    if (link.IsSeparator) return;
    if (link.Path == "EXIT") { Application.RequestStop(); return; }
    try { Process.Start(new ProcessStartInfo(link.Path) { UseShellExecute = true }); }
    catch { }
}

// ── Timers ────────────────────────────────────────────────────────────────────

Application.AddTimeout(TimeSpan.FromSeconds(1), () =>
{
    clockLbl.Text = DateTime.Now.ToString(cfg.Clock24Hr ? "HH:mm:ss" : "hh:mm:ss tt");
    return true;
});

PerformanceCounter? cpuCounter = null;
PerformanceCounter? ramCounter = null;
try
{
    cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
    cpuCounter.NextValue();
    ramCounter = new PerformanceCounter("Memory", "Available MBytes");
}
catch { }

Application.AddTimeout(TimeSpan.FromSeconds(3), () =>
{
    try
    {
        if (cpuCounter is not null)
        {
            float cpu = cpuCounter.NextValue();
            cpuLbl.Text = $"CPU : {cpu,3:F0}%  [{MiniBar(cpu, 100, 10)}]";
        }

        if (ramCounter is not null)
            ramLbl.Text = $"RAM : {ramCounter.NextValue() / 1024:F1} GB available";
    }
    catch { }
    return true;
});

Application.AddTimeout(TimeSpan.FromSeconds(20), () => { _ = RefreshNetworkAsync(); return true; });
Application.AddTimeout(TimeSpan.FromMinutes(10), () => { _ = RefreshWeatherAsync(); return true; });

_ = RefreshWeatherAsync();
_ = RefreshNetworkAsync();

// ── Run ───────────────────────────────────────────────────────────────────────


Application.Run(top);
Application.Shutdown();
cpuCounter?.Dispose();
ramCounter?.Dispose();

// ── Helpers ───────────────────────────────────────────────────────────────────

static string MiniBar(float value, float max, int width)
{
    int filled = Math.Clamp((int)(value / max * width), 0, width);
    return new string('█', filled) + new string('░', width - filled);
}

async Task RefreshWeatherAsync()
{
    try
    {
        var result = await http.GetStringAsync($"https://wttr.in/{cfg.WeatherZip}?format=3");
        Application.Invoke(() => weatherLbl.Text = result.Trim());
    }
    catch { }
}

async Task RefreshNetworkAsync()
{
    try
    {
        bool connected = NetworkInterface.GetIsNetworkAvailable();
        string status  = connected ? "● Connected" : "○ Disconnected";
        string adapter = "--";
        string netName = "--";
        string localIp = "--";
        string latency = "--";

        if (connected)
        {
            var primary = NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault(n =>
                    n.OperationalStatus == OperationalStatus.Up &&
                    n.NetworkInterfaceType != NetworkInterfaceType.Loopback);

            if (primary is not null)
            {
                adapter = primary.NetworkInterfaceType.ToString();
                netName = primary.Name;
                localIp = primary.GetIPProperties().UnicastAddresses
                    .FirstOrDefault(a =>
                        a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    ?.Address.ToString() ?? "--";
            }

            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(cfg.PingTarget, 2000);
                latency = reply.Status == IPStatus.Success
                    ? $"{reply.RoundtripTime}ms ({cfg.PingTarget})"
                    : $"Timeout ({cfg.PingTarget})";
            }
            catch { latency = "Ping error"; }

            if (publicIp == "--" || (DateTime.UtcNow - lastPublicIpFetch).TotalMinutes >= 5)
            {
                try
                {
                    publicIp = (await http.GetStringAsync("https://api.ipify.org")).Trim();
                    lastPublicIpFetch = DateTime.UtcNow;
                }
                catch { }
            }
        }

        Application.Invoke(() =>
        {
            netStatusLbl.Text  = $"Status   : {status}";
            netNameLbl.Text    = $"Network  : {netName}";
            netAdapterLbl.Text = $"Adapter  : {adapter}";
            netLatencyLbl.Text = $"Latency  : {latency}";
            netLocalLbl.Text   = $"Local IP : {localIp}";
            netPublicLbl.Text  = $"Public   : {publicIp}";
        });
    }
    catch { }
}
