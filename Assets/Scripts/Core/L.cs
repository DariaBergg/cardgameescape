using System.Collections.Generic;
using UnityEngine;

// Локализация. Ключ — русский текст, как он написан в коде и данных.
// Таблица переводов: Assets/Resources/Localization/<lang>.tsv, строки «русский<TAB>перевод», \n внутри — перенос.
public static class L
{
    const string PrefKey = "language";
    static string language = "ru";
    static Dictionary<string, string> table = new Dictionary<string, string>();
    static bool loaded;

    public static string Language
    {
        get { EnsureLoaded(); return language; }
        set
        {
            language = value;
            PlayerPrefs.SetString(PrefKey, value);
            PlayerPrefs.Save();
            loaded = false;
            EnsureLoaded();
        }
    }

    public static bool IsRussian => Language == "ru";

    static void EnsureLoaded()
    {
        if (loaded) return;
        loaded = true;
        language = PlayerPrefs.GetString(PrefKey, language);
        table = new Dictionary<string, string>();
        if (language == "ru") return;
        var asset = Resources.Load<TextAsset>("Localization/" + language);
        if (asset == null) { Debug.LogWarning("Localization: no table for " + language); return; }
        foreach (var raw in asset.text.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            if (line.Length == 0 || line.StartsWith("#")) continue;
            int tab = line.IndexOf('\t');
            if (tab < 0) continue;
            string key = Unescape(line.Substring(0, tab));
            string value = Unescape(line.Substring(tab + 1));
            if (value.Length > 0) table[key] = value;
        }
    }

    static string Unescape(string s) => s.Replace("\\n", "\n").Replace("\\t", "\t");

    // Перевод строки; если перевода нет — возвращается исходный русский текст
    public static string T(string ru)
    {
        if (string.IsNullOrEmpty(ru)) return ru;
        EnsureLoaded();
        return table.TryGetValue(ru, out var en) ? en : ru;
    }

    // Перевод с подстановкой: L.F("Нанести {0} урона", 4)
    public static string F(string ru, params object[] args)
    {
        var fmt = T(ru);
        try { return string.Format(fmt, args); }
        catch (System.FormatException) { return fmt; }
    }
}
