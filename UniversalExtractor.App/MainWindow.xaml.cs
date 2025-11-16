// Copyright (c) 2024.
// This file is part of UniversalExtractor and is licensed under the GNU General Public License v3.0.
// See the LICENSE file distributed with this work for additional information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using UniversalExtractor.App.Models;
using UniversalExtractor.App.Services;
using DrawingIcon = System.Drawing.Icon;

namespace UniversalExtractor.App;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly string _defaultDropMessage = "Drop a supported document here.";
    private string? _selectedFilePath;
    private string _dropMessage;
    private ImageSource? _selectedFileIcon;
    private ExtractionDefinition? _selectedExtraction;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        _dropMessage = _defaultDropMessage;
    }

    public IReadOnlyList<ExtractionDefinition> ExtractionOptions => ExtractionDefinition.All;

    public ExtractionDefinition? SelectedExtraction
    {
        get => _selectedExtraction;
        set
        {
            if (_selectedExtraction != value)
            {
                _selectedExtraction = value;
                OnPropertyChanged(nameof(SelectedExtraction));
            }
        }
    }

    public string DropMessage
    {
        get => _dropMessage;
        private set
        {
            if (_dropMessage != value)
            {
                _dropMessage = value;
                OnPropertyChanged(nameof(DropMessage));
            }
        }
    }

    public ImageSource? SelectedFileIcon
    {
        get => _selectedFileIcon;
        private set
        {
            if (!Equals(_selectedFileIcon, value))
            {
                _selectedFileIcon = value;
                OnPropertyChanged(nameof(SelectedFileIcon));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void DropZone_DragEnter(object sender, DragEventArgs e) => EvaluateDragData(e);

    private void DropZone_DragOver(object sender, DragEventArgs e) => EvaluateDragData(e);

    private void EvaluateDragData(DragEventArgs e)
    {
        if (TryGetFirstFile(e.Data, out var filePath) && DocumentTextExtractor.IsSupported(filePath))
        {
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }

        e.Handled = true;
    }

    private async void DropZone_Drop(object sender, DragEventArgs e)
    {
        if (!TryGetFirstFile(e.Data, out var filePath))
        {
            return;
        }

        await SelectFileAsync(filePath);
    }

    private async Task SelectFileAsync(string filePath)
    {
        if (!DocumentTextExtractor.IsSupported(filePath))
        {
            MessageBox.Show(this, "Unsupported file type selected.", "Universal Extractor", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _selectedFilePath = filePath;
        DropMessage = Path.GetFileName(filePath);
        SelectedFileIcon = await Task.Run(() => LoadFileIcon(filePath));
    }

    private static bool TryGetFirstFile(IDataObject dataObject, out string filePath)
    {
        filePath = string.Empty;
        if (!dataObject.GetDataPresent(DataFormats.FileDrop))
        {
            return false;
        }

        if (dataObject.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
        {
            filePath = files.First();
            return true;
        }

        return false;
    }

    private static ImageSource? LoadFileIcon(string filePath)
    {
        try
        {
            using DrawingIcon? icon = DrawingIcon.ExtractAssociatedIcon(filePath);
            if (icon == null)
            {
                return null;
            }

            var imageSource = Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromWidthAndHeight(64, 64));
            imageSource.Freeze();
            return imageSource;
        }
        catch
        {
            return null;
        }
    }

    private async void ExtractButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_selectedFilePath))
        {
            MessageBox.Show(this, "Please drop a document first.", "Universal Extractor", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (SelectedExtraction is null)
        {
            MessageBox.Show(this, "Please choose the kind of data to extract.", "Universal Extractor", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            Mouse.OverrideCursor = Cursors.Wait;
            var text = await DocumentTextExtractor.ReadAsTextAsync(_selectedFilePath);
            var matches = SelectedExtraction.ExtractMatches(text);
            var orderedUnique = matches
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (orderedUnique.Count == 0)
            {
                MessageBox.Show(this, "No matching data found. An empty output will be created.", "Universal Extractor", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            var saveDialog = new SaveFileDialog
            {
                Title = "Save extracted data",
                Filter = "Text file (*.txt)|*.txt|All files (*.*)|*.*",
                FileName = $"{Path.GetFileNameWithoutExtension(_selectedFilePath)}_{SelectedExtraction.Name.Replace(' ', '_')}.txt"
            };

            if (saveDialog.ShowDialog(this) == true)
            {
                var content = string.Join(Environment.NewLine, orderedUnique);
                await File.WriteAllTextAsync(saveDialog.FileName, content);
                MessageBox.Show(this, "Extraction file created successfully.", "Universal Extractor", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Extraction failed: {ex.Message}", "Universal Extractor", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    private void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
