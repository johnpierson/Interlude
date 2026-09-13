using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Autodesk.DesignScript.Runtime;
using Interlude.Model;
using Microsoft.Win32;

namespace Interlude.Rendering.Wpf.Controls;

/// <summary>
/// A path entry with a Browse button, used for both files and folders.
///
/// The folder case uses <see cref="OpenFolderDialog"/> where it exists and falls back to picking
/// a file inside the wanted folder otherwise, which avoids the usual alternatives: a Windows
/// Forms reference, or the old shell folder browser that looks two decades out of place.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class PathBox : Grid
{
    private readonly TextBox _entry;
    private readonly FilePickerElement? _fileOptions;
    private readonly bool _isFolder;

    private bool _isWriting;
    private IReadOnlyList<string>? _selectedPaths;

    internal PathBox(FilePickerElement? fileOptions, string? initialDirectory, bool isFolder)
    {
        _fileOptions = fileOptions;
        _isFolder = isFolder;
        InitialDirectory = initialDirectory;

        ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _entry = new TextBox { VerticalContentAlignment = VerticalAlignment.Center };
        _entry.TextChanged += (_, _) =>
        {
            if (!_isWriting)
            {
                _selectedPaths = null;
                PathChanged?.Invoke(this, EventArgs.Empty);
            }
        };
        SetColumn(_entry, 0);
        Children.Add(_entry);

        Button browse = new()
        {
            Content = "Browse…",
            Margin = new Thickness(6, 0, 0, 0),
            Padding = new Thickness(10, 0, 10, 0),
            MinWidth = 76,
        };
        browse.Click += OnBrowse;
        SetColumn(browse, 1);
        Children.Add(browse);
    }

    /// <summary>Raised when the path changes through typing or browsing.</summary>
    internal event EventHandler? PathChanged;

    internal string? InitialDirectory { get; }

    /// <summary>The path text, or the paths joined by a semicolon for a multi-select picker.</summary>
    internal string Text
    {
        get => _entry.Text;
        set
        {
            if (string.Equals(_entry.Text, value, StringComparison.Ordinal))
            {
                return;
            }

            _isWriting = true;
            try
            {
                _selectedPaths = null;
                _entry.Text = value ?? string.Empty;
            }
            finally
            {
                _isWriting = false;
            }
        }
    }

    /// <summary>The selected paths, split out of the text.</summary>
    internal IReadOnlyList<string> Paths => _selectedPaths ?? ParsePaths(_entry.Text);

    /// <summary>Replaces the selection with a list of paths.</summary>
    internal void SetPaths(IEnumerable<string> paths)
    {
        string[] selected = paths
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToArray();

        _selectedPaths = selected;
        _isWriting = true;
        try
        {
            _entry.Text = string.Join("; ", selected);
        }
        finally
        {
            _isWriting = false;
        }
    }

    private static IReadOnlyList<string> ParsePaths(string text)
        => text.Split(';')
            .Select(part => part.Trim())
            .Where(part => part.Length > 0)
            .ToList();

    private void OnBrowse(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_isFolder)
            {
                BrowseForFolder();
                return;
            }

            BrowseForFile();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            // A dialog that cannot open its starting folder must not take the form down with it.
            MessageBox.Show(
                Window.GetWindow(this),
                "The file browser could not be opened: " + ex.Message,
                "Interlude",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void BrowseForFolder()
    {
        OpenFolderDialog dialog = new()
        {
            Multiselect = false,
        };

        string? start = FirstExistingDirectory();
        if (start is not null)
        {
            dialog.InitialDirectory = start;
        }

        if (dialog.ShowDialog(Window.GetWindow(this)) == true)
        {
            Text = dialog.FolderName;
            PathChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void BrowseForFile()
    {
        FileDialog dialog = _fileOptions?.IsSaveDialog == true
            ? new SaveFileDialog { DefaultExt = _fileOptions.DefaultExtension ?? string.Empty }
            : new OpenFileDialog { Multiselect = _fileOptions?.AllowMultiple == true };

        dialog.Filter = string.IsNullOrWhiteSpace(_fileOptions?.Filter) ? "All files|*.*" : _fileOptions!.Filter;

        string? start = FirstExistingDirectory();
        if (start is not null)
        {
            dialog.InitialDirectory = start;
        }

        if (dialog.ShowDialog(Window.GetWindow(this)) != true)
        {
            return;
        }

        if (dialog is OpenFileDialog { Multiselect: true } multi)
        {
            SetPaths(multi.FileNames);
        }
        else
        {
            Text = dialog.FileName;
        }

        PathChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Starts the dialog where the user last was, falling back to the author's suggestion.
    /// </summary>
    private string? FirstExistingDirectory()
    {
        foreach (string candidate in Paths)
        {
            try
            {
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }

                string? parent = Path.GetDirectoryName(candidate);
                if (!string.IsNullOrEmpty(parent) && Directory.Exists(parent))
                {
                    return parent;
                }
            }
            catch (ArgumentException)
            {
                // Not a usable path; try the next one.
            }
        }

        return !string.IsNullOrWhiteSpace(InitialDirectory) && Directory.Exists(InitialDirectory)
            ? InitialDirectory
            : null;
    }
}
