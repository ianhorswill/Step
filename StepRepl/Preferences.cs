using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Avalonia;

namespace StepRepl;

public sealed class Preferences
{
    public static readonly string ApplicationName = "Step Repl";
    public static string PreferencesPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), $"{ApplicationName} Preferences.json");

    private static Dictionary<string, string> _prefs = LoadFromDisk();
    
    public static string Get(string key, string defaultValue)
    {
        return _prefs.GetValueOrDefault(key, defaultValue);
    }
    
    public static void Set(string key, string value)
    {
        _prefs[key] = value;
        SaveToDisk();
    }
    
    public static Dictionary<string, string> LoadFromDisk()
    {
        string json;
        try
        {
            json = File.ReadAllText(PreferencesPath);
        }
        catch (FileNotFoundException)
        {
            return new Dictionary<string, string>();
        }
        catch (Exception e)
        {
            Console.WriteLine("An error occurred while reading application preferences:");
            Console.WriteLine(e.Message);
            return new Dictionary<string, string>();
        }

        var deserialized = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        return deserialized ?? new Dictionary<string, string>();
    }
    
    public static async void SaveToDisk()
    {
        string json = JsonSerializer.Serialize(_prefs);
        await File.WriteAllTextAsync(PreferencesPath, json);
    }
}