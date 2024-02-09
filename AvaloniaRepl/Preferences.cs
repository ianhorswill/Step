using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace AvaloniaRepl;

public sealed class Preferences
{
    private static Dictionary<string, string> _prefs = new();
    
    public static string Get(string key, string defaultValue)
    {
        return _prefs.GetValueOrDefault(key, defaultValue);
    }
    
    public static void Set(string key, string value)
    {
        _prefs[key] = value;
        SaveToDisk();
    }
    
    public static void LoadFromDisk()
    {
        string json;
        try
        {
            json = File.ReadAllText("preferences.json");
        }
        catch (Exception e)
        {
            Console.WriteLine("An error occurred while reading application preferences:");
            Console.WriteLine(e.Message);
            return;
        }

        var deserialized = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        if (deserialized != null)
        {
            _prefs = deserialized;
        }
    }
    
    public static async void SaveToDisk()
    {
        string json = JsonSerializer.Serialize(_prefs);
        await File.WriteAllTextAsync("preferences.json", json);
    }
}