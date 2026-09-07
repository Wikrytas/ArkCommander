namespace ArkCommander.Models;

public class AppConfig
{
    public AppSettings Settings { get; set; } = new();
    public CommandTemplates Templates { get; set; } = new();

    public List<BuyItem> BuyItems { get; set; } = new();
    public List<KitItem> KitItems { get; set; } = new();
    public List<SaleItem> SaleItems { get; set; } = new();
    public List<CommandItem> Commands { get; set; } = new();
    public List<TrackItem> TrackItems { get; set; } = new();
    public List<DinoAtlas> Atlas { get; set; } = new();
}

public class AppSettings
{
    public string ChatOpenKey { get; set; } = "Enter";
    public bool ActivateGameWindow { get; set; } = true;

    public string GameProcessName { get; set; } = "ShooterGame";
    public string GameWindowTitle { get; set; } = "ARK: Survival Evolved";

    public int DelayBeforeChatOpenMs { get; set; } = 150;
    public int DelayAfterChatOpenMs { get; set; } = 300;
    public int DelayAfterTypingMs { get; set; } = 120;
    public int DelayBetweenCommandsMs { get; set; } = 300;

    public int TrackDelayMs { get; set; } = 1500;
    public TrackCaptureSettings TrackCapture { get; set; } = new();
    public CoordCaptureSettings CoordCapture { get; set; } = new();

    public string MapName { get; set; } = "Ragnarok";
    public double MapSizeMeters { get; set; } = 13100;
    public MapCalibSettings MapCalib { get; set; } = new();
    public OverlaySettings Overlay { get; set; } = new();
    public string ChatKey { get; set; } = "Enter";
    public bool OverlayOnStartup { get; set; } = false;
    public bool OverlayTopmost { get; set; } = true;
    public bool CloseToTray { get; set; } = true;
    public bool VoiceEnabled { get; set; } = true;
    public string VoiceHotkey { get; set; } = "F9";
    public bool VoiceHoldMode { get; set; } = true;
    public bool VoiceAssistantEnabled { get; set; } = true;
    public bool VoiceConfirm { get; set; } = true;
}

public class OverlaySettings
{
    public string Position { get; set; } = "TopCenter";
    public int Margin { get; set; } = 8;
    public double XPercent { get; set; } = 0.4;
    public double YPercent { get; set; } = 0.05;
    public double Opacity { get; set; } = 0.75;
    public double FontSize { get; set; } = 12;
    public string FontFamily { get; set; } = "Segoe UI";
    public string TextColor { get; set; } = "#E8ECF4";
    public string HotkeyColor { get; set; } = "#3FA7FF";
    public string BackgroundColor { get; set; } = "#AA0B0E14";
    public bool ShowHotkeys { get; set; } = true;
    public List<string> Commands { get; set; } = new();
}

public class TrackCaptureSettings
{
    public double XPercent { get; set; } = 0.30;
    public double YPercent { get; set; } = 0.0;
    public double WidthPercent { get; set; } = 0.40;
    public double HeightPercent { get; set; } = 0.25;
}


public class CoordCaptureSettings
{
    public double XPercent { get; set; } = 0.60;
    public double YPercent { get; set; } = 0.0;
    public double WidthPercent { get; set; } = 0.40;
    public double HeightPercent { get; set; } = 0.05;
}

public class MapCalibSettings
{
    public double LatTop { get; set; } = 0;
    public double LatBottom { get; set; } = 100;
    public double LonLeft { get; set; } = 0;
    public double LonRight { get; set; } = 100;
}

public class CommandTemplates
{
    public string BuyCommandTemplate { get; set; } = "/купить {id} {qty}";
    public string KitCommandTemplate { get; set; } = "/kit {id}";
    public string SaleCommandTemplate { get; set; } = "/продажа {id}";
    public string TransferCommandTemplate { get; set; } = "/передать {player} {amount}";
    public string TrackCommandTemplate { get; set; } = "/track {name}";
}

public class BuyItem
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public int Qty { get; set; }
    public decimal Price { get; set; }
    public string Icon { get; set; } = "";
    public string Cat { get; set; } = "";
}

public class KitItem
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Qty { get; set; } = "";
    public decimal Price { get; set; }
}

public class SaleItem
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Qty { get; set; } = "";
    public decimal Price { get; set; }
}

public class TrackItem
{
    public string Name { get; set; } = "";
}

public class CommandItem
{
    public string Name { get; set; } = "";
    public string Command { get; set; } = "";
    public string Hotkey { get; set; } = "";
    public string Mod { get; set; } = "None";

    public decimal Price { get; set; }

    public string Group { get; set; } = "";
    public string Cat { get; set; } = "";
    public string Short { get; set; } = "";

    public bool ShowOnPanel { get; set; }

    public bool IsKit { get; set; }
    public bool IsBuy { get; set; }
    public bool IsSale { get; set; }
    public bool IsTransfer { get; set; }
    public bool IsTrack { get; set; }
}

public class PickerEntry
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Details { get; set; } = "";
    public string PriceText { get; set; } = "";
    public string IconPath { get; set; } = "";
    public bool HasIcon => !string.IsNullOrEmpty(IconPath);
}

public class DinoAtlas
{
    public string Name { get; set; } = "";
    public List<SpawnPoint> Points { get; set; } = new();
}

public class SpawnPoint
{
    public double Lat { get; set; }
    public double Lon { get; set; }
    public double Lat2 { get; set; }
    public double Lon2 { get; set; }
    public string Rarity { get; set; } = "rare";
    public string Creature { get; set; } = "";
}

public class ObeliskPoint
{
    public string Name { get; set; } = "";
    public double Lat { get; set; }
    public double Lon { get; set; }
    public string Color { get; set; } = "white";
}









