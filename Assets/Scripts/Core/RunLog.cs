using System;
using System.IO;
using UnityEngine;

// Журнал прохождения: простой текстовый файл, чтобы потом разобрать забег
public static class RunLog
{
    static string Path => System.IO.Path.Combine(Application.persistentDataPath, "runlog.txt");

    public static void Write(string line)
    {
        try
        {
            var gm = GameManager.Instance;
            string hp = gm != null ? $"HP {gm.currentHP}/{gm.MaxHP}" : "";
            File.AppendAllText(Path, $"{DateTime.Now:HH:mm:ss} | {hp} | {line}\n");
        }
        catch (Exception) { }
    }

    public static void NewRun(string character)
    {
        Write($"=== Новый забег: {character} ===");
    }
}
