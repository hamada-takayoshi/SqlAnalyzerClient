using Microsoft.Win32;
using System.IO;

namespace SqlAnalyzer.App.Services;

public sealed class ExportService
{
    public bool SaveMermaidMarkdown(string mermaidText)
    {
        SaveFileDialog dialog = new()
        {
            Title = "Mermaid Markdownを保存",
            Filter = "Markdownファイル (*.md)|*.md|すべてのファイル (*.*)|*.*",
            FileName = $"diagram_{DateTime.Now:yyyyMMdd_HHmmss}.md",
            AddExtension = true,
            DefaultExt = ".md"
        };

        if (dialog.ShowDialog() != true)
        {
            return false;
        }

        File.WriteAllText(dialog.FileName, mermaidText, new System.Text.UTF8Encoding(false));
        return true;
    }

    public bool SavePng(byte[] pngBytes)
    {
        SaveFileDialog dialog = new()
        {
            Title = "図PNGを保存",
            Filter = "PNGファイル (*.png)|*.png|すべてのファイル (*.*)|*.*",
            FileName = $"diagram_{DateTime.Now:yyyyMMdd_HHmmss}.png",
            AddExtension = true,
            DefaultExt = ".png"
        };

        if (dialog.ShowDialog() != true)
        {
            DebugLog.Write("SavePng canceled by user.");
            return false;
        }

        try
        {
            File.WriteAllBytes(dialog.FileName, pngBytes);
            DebugLog.Write($"SavePng success: path={dialog.FileName}, bytes={pngBytes.Length}");
            return true;
        }
        catch (Exception ex)
        {
            DebugLog.Write($"SavePng error: path={dialog.FileName}, bytes={pngBytes.Length}, ex={ex}");
            return false;
        }
    }
}
