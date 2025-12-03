namespace PageCounterPro.Core.Services;

using System.Runtime.CompilerServices;
using PageCounterPro.Core.Interfaces;
using PageCounterPro.Core.Models;
using Microsoft.Extensions.Logging;

/// <summary>
/// Service for scanning folders and enumerating files.
/// </summary>
public sealed class FileScanner : IFileScanner
{
    private readonly ILogger<FileScanner> _logger;

    public FileScanner(ILogger<FileScanner> logger)
    {
        _logger = logger;
    }

    public async Task<int> GetTotalFileCountAsync(ScanOptions options, CancellationToken cancellationToken = default)
    {
        return await GetTotalFileCountAsync(options, null, cancellationToken);
    }

    public async Task<int> GetTotalFileCountAsync(ScanOptions options, IProgress<ScanProgress>? progress, CancellationToken cancellationToken = default)
    {
        try
        {
            var searchOption = options.IncludeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var directory = new DirectoryInfo(options.RootFolderPath);

            if (!directory.Exists)
            {
                _logger.LogWarning("Directory does not exist: {Path}", options.RootFolderPath);
                return 0;
            }

            var files = await Task.Run(() =>
            {
                var allFiles = directory.EnumerateFiles("*", searchOption);

                // Apply depth filter if specified
                if (options.MaxDepth.HasValue)
                {
                    var rootDepth = GetPathDepth(options.RootFolderPath);
                    allFiles = allFiles.Where(f => GetPathDepth(f.DirectoryName!) - rootDepth <= options.MaxDepth.Value);
                }

                // Apply file type filter if specified
                if (options.FileTypeFilter?.Any() == true)
                {
                    var extensions = options.FileTypeFilter.Select(e => $".{e.ToLowerInvariant()}").ToHashSet();
                    allFiles = allFiles.Where(f => extensions.Contains(f.Extension.ToLowerInvariant()));
                }

                // Count with progress reporting
                var count = 0;
                foreach (var file in allFiles)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    count++;

                    // Report progress every 100 files during enumeration
                    if (count % 100 == 0)
                    {
                        progress?.Report(new ScanProgress
                        {
                            IsEnumerating = true,
                            ProcessedCount = count,
                            TotalCount = 0,
                            StatusMessage = $"Discovering files... {count} found"
                        });
                    }
                }

                return count;
            }, cancellationToken);

            _logger.LogInformation("Found {Count} files in {Path}", files, options.RootFolderPath);
            return files;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting files in {Path}", options.RootFolderPath);
            return 0;
        }
    }

    public async IAsyncEnumerable<FileInfo> ScanFilesAsync(
        ScanOptions options,
        IProgress<ScanProgress>? progress = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var directory = new DirectoryInfo(options.RootFolderPath);

        if (!directory.Exists)
        {
            _logger.LogWarning("Directory does not exist: {Path}", options.RootFolderPath);
            yield break;
        }

        var searchOption = options.IncludeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        IEnumerable<FileInfo> files;

        try
        {
            files = directory.EnumerateFiles("*", searchOption);

            // Apply depth filter if specified
            if (options.MaxDepth.HasValue)
            {
                var rootDepth = GetPathDepth(options.RootFolderPath);
                files = files.Where(f => GetPathDepth(f.DirectoryName!) - rootDepth <= options.MaxDepth.Value);
            }

            // Apply file type filter if specified
            if (options.FileTypeFilter?.Any() == true)
            {
                var extensions = options.FileTypeFilter.Select(e => $".{e.ToLowerInvariant()}").ToHashSet();
                files = files.Where(f => extensions.Contains(f.Extension.ToLowerInvariant()));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enumerating files in {Path}", options.RootFolderPath);
            yield break;
        }

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            FileInfo? safeFile = null;
            try
            {
                // Verify the file still exists and is accessible
                if (file.Exists)
                {
                    safeFile = file;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cannot access file: {Path}", file.FullName);
            }

            if (safeFile != null)
            {
                // Add a small yield to keep the UI responsive
                await Task.Yield();
                yield return safeFile;
            }
        }
    }

    private static int GetPathDepth(string path)
    {
        return path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Count(s => !string.IsNullOrEmpty(s));
    }
}
