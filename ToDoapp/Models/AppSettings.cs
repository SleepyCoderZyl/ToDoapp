using System;
using System.Text.Json.Serialization;

namespace ToDoapp.Models;

public class AppSettings
{
    [JsonPropertyName("widgetOpacity")]
    public double WidgetOpacity { get; set; } = 0.8;

    [JsonPropertyName("widgetContentOpacity")]
    public double WidgetContentOpacity { get; set; } = 1.0;

    [JsonPropertyName("windowWidth")]
    public double WindowWidth { get; set; } = 420;

    [JsonPropertyName("windowHeight")]
    public double WindowHeight { get; set; } = 700;

    [JsonPropertyName("windowLeft")]
    public double WindowLeft { get; set; } = 0;

    [JsonPropertyName("windowTop")]
    public double WindowTop { get; set; } = 0;

    [JsonPropertyName("widgetModeWidth")]
    public double WidgetModeWidth { get; set; } = 280;

    [JsonPropertyName("widgetModeHeight")]
    public double WidgetModeHeight { get; set; } = 360;

    [JsonPropertyName("widgetModeLeft")]
    public double WidgetModeLeft { get; set; } = 0;

    [JsonPropertyName("widgetModeTop")]
    public double WidgetModeTop { get; set; } = 0;

    [JsonPropertyName("isFirstRun")]
    public bool IsFirstRun { get; set; } = true;

    [JsonPropertyName("autoStart")]
    public bool AutoStart { get; set; } = false;

    [JsonPropertyName("widgetAlwaysOnTop")]
    public bool WidgetAlwaysOnTop { get; set; } = true;

    [JsonPropertyName("lastUpdated")]
    public DateTime LastUpdated { get; set; } = DateTime.Now;

    [JsonPropertyName("hotKeyModifiers")]
    public uint HotKeyModifiers { get; set; } = 0x0002 | 0x0004 | 0x0001;

    [JsonPropertyName("hotKeyKey")]
    public uint HotKeyKey { get; set; } = 0x5A;

    [JsonPropertyName("startInWidgetMode")]
    public bool StartInWidgetMode { get; set; } = true;

    [JsonPropertyName("autoArchiveDays")]
    public int AutoArchiveDays { get; set; } = 7;
}
