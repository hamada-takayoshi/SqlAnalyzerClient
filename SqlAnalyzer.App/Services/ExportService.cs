using Microsoft.Win32;
using System.IO;

namespace SqlAnalyzer.App.Services;

public sealed class ExportService
{
    public bool SaveMermaidMarkdown(string mermaidText)
    {
        SaveFileDialog dialog = new()
        {
            Title = "Save Mermaid Markdown",
            Filter = "Markdown files (*.md)|*.md|All files (*.*)|*.*",
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
            Title = "Save Diagram PNG",
            Filter = "PNG files (*.png)|*.png|All files (*.*)|*.*",
            FileName = $"diagram_{DateTime.Now:yyyyMMdd_HHmmss}.png",
            AddExtension = true,
            DefaultExt = ".png"
        };

        if (dialog.ShowDialog() != true)
        {
            return false;
        }

        File.WriteAllBytes(dialog.FileName, pngBytes);
        return true;
    }
}
