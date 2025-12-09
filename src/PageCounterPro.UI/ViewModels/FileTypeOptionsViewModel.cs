namespace PageCounterPro.UI.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PageCounterPro.Core.Models;
using System.Collections.ObjectModel;

/// <summary>
/// ViewModel for a single file type option in the dialog.
/// </summary>
public partial class FileTypeOptionItem : ObservableObject
{
    [ObservableProperty]
    private string _extension = string.Empty;

    [ObservableProperty]
    private string _category = string.Empty;

    [ObservableProperty]
    private bool _countAs1Page = true;

    [ObservableProperty]
    private bool _showDuration = true;

    [ObservableProperty]
    private bool _canShowDuration;

    [ObservableProperty]
    private bool _metadataReadable = true;

    [ObservableProperty]
    private int _fileCount;
}

/// <summary>
/// ViewModel for a category group in the file type options dialog.
/// </summary>
public partial class FileTypeCategoryGroup : ObservableObject
{
    [ObservableProperty]
    private string _categoryName = string.Empty;

    [ObservableProperty]
    private string _categoryIcon = string.Empty;

    public ObservableCollection<FileTypeOptionItem> FileTypes { get; } = new();

    public int TotalFiles => FileTypes.Sum(f => f.FileCount);
}

/// <summary>
/// ViewModel for the FileTypeOptionsDialog.
/// </summary>
public partial class FileTypeOptionsViewModel : ObservableObject
{
    public ObservableCollection<FileTypeCategoryGroup> Categories { get; } = new();

    [ObservableProperty]
    private bool _dialogResult;

    [ObservableProperty]
    private string _title = "File Type Options";

    [ObservableProperty]
    private string _description = "The following file types were found. Configure how they should be counted:";

    public Action? CloseAction { get; set; }

    [RelayCommand]
    private void Ok()
    {
        DialogResult = true;
        CloseAction?.Invoke();
    }

    [RelayCommand]
    private void Cancel()
    {
        DialogResult = false;
        CloseAction?.Invoke();
    }

    /// <summary>
    /// Populates the dialog with unknown file types found during scan.
    /// </summary>
    /// <param name="unknownExtensions">Dictionary of extension -> (category, fileCount, metadataReadable)</param>
    /// <param name="existingPreferences">Existing user preferences to pre-populate values</param>
    public void PopulateFromScan(
        Dictionary<string, (string Category, int FileCount, bool MetadataReadable)> unknownExtensions,
        Dictionary<string, FileTypePreference> existingPreferences)
    {
        Categories.Clear();

        var grouped = unknownExtensions
            .GroupBy(x => x.Value.Category)
            .OrderBy(g => g.Key);

        foreach (var group in grouped)
        {
            var categoryGroup = new FileTypeCategoryGroup
            {
                CategoryName = group.Key,
                CategoryIcon = GetCategoryIcon(group.Key)
            };

            foreach (var ext in group.OrderBy(x => x.Key))
            {
                var item = new FileTypeOptionItem
                {
                    Extension = ext.Key,
                    Category = ext.Value.Category,
                    FileCount = ext.Value.FileCount,
                    MetadataReadable = ext.Value.MetadataReadable,
                    CanShowDuration = ext.Value.Category == "Video",
                    CountAs1Page = true,
                    ShowDuration = ext.Value.MetadataReadable
                };

                // Apply existing preferences if available
                if (existingPreferences.TryGetValue(ext.Key, out var pref))
                {
                    item.CountAs1Page = pref.CountAs1Page;
                    item.ShowDuration = pref.ShowDuration;
                }

                categoryGroup.FileTypes.Add(item);
            }

            Categories.Add(categoryGroup);
        }
    }

    /// <summary>
    /// Gets the configured preferences from the dialog.
    /// </summary>
    public Dictionary<string, FileTypePreference> GetPreferences()
    {
        var result = new Dictionary<string, FileTypePreference>();

        foreach (var category in Categories)
        {
            foreach (var item in category.FileTypes)
            {
                result[item.Extension] = new FileTypePreference
                {
                    CountAs1Page = item.CountAs1Page,
                    ShowDuration = item.ShowDuration,
                    IsConfigured = true,
                    Category = item.Category
                };
            }
        }

        return result;
    }

    private static string GetCategoryIcon(string category) => category switch
    {
        "Video" => "🎬",
        "Image" => "🖼️",
        "Audio" => "🎵",
        "Document" => "📄",
        _ => "📁"
    };
}
