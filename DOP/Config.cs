using Terminal.Gui;

namespace DOP;

public class Config
{
    public Color  AccentColor { get; set; } = Color.Cyan;
    public Color  GridColor   { get; set; } = Color.DarkGray;
    public Color  Background  { get; set; } = Color.Black;
    public bool   Clock24Hr   { get; set; } = false;
    public string WeatherZip  { get; set; } = "78628";
    public string PingTarget  { get; set; } = "8.8.8.8";
    public List<LinkEntry> Links { get; set; } = [];

    public static Config Load(string path)
    {
        var cfg = new Config();
        if (!File.Exists(path)) return cfg;

        string? section = null;

        foreach (var rawLine in File.ReadLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith(';') || line.StartsWith('#')) continue;

            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                section = line[1..^1];
                continue;
            }

            var eq = line.IndexOf('=');
            if (eq < 0) continue;
            var key = line[..eq].Trim();
            var val = line[(eq + 1)..].Trim();

            switch (section)
            {
                case "General":
                    switch (key)
                    {
                        case "AccentColor": cfg.AccentColor = ParseColor(val, Color.Cyan);     break;
                        case "GridColor":   cfg.GridColor   = ParseColor(val, Color.DarkGray); break;
                        case "Background":  cfg.Background  = ParseColor(val, Color.Black);    break;
                        case "ClockFormat": cfg.Clock24Hr   = val == "24";                     break;
                    }
                    break;

                case "Weather":
                    if (key == "WeatherZip") cfg.WeatherZip = val;
                    break;

                case "Network":
                    if (key == "PingTarget") cfg.PingTarget = val;
                    break;

                case "Links":
                    if (!key.StartsWith("Link")) break;
                    var parts = val.Split('|');
                    var label = parts.Length > 0 ? parts[0].Trim() : "";
                    if (label == "---")
                    {
                        cfg.Links.Add(new LinkEntry { IsSeparator = true });
                    }
                    else
                    {
                        cfg.Links.Add(new LinkEntry
                        {
                            Label    = label,
                            Path     = parts.Length > 1 ? parts[1].Trim() : "",
                            Category = parts.Length > 2 ? parts[2].Trim() : "",
                            Hotkey   = parts.Length > 3 ? parts[3].Trim() : "",
                        });
                    }
                    break;
            }
        }
        return cfg;
    }

    static Color ParseColor(string val, Color fallback) => val switch
    {
        "Black"    => Color.Black,
        "DarkGray" => Color.DarkGray,
        "Cyan"     => Color.Cyan,
        "Green"    => Color.Green,
        "White"    => Color.White,
        "Red"      => Color.Red,
        "Yellow"   => Color.Yellow,
        _          => fallback,
    };
}

public class LinkEntry
{
    public string Label       { get; set; } = "";
    public string Path        { get; set; } = "";
    public string Category    { get; set; } = "";
    public string Hotkey      { get; set; } = "";
    public bool   IsSeparator { get; set; } = false;
}
