namespace PageCounterPro.Core.Services;

using PageCounterPro.Core.Interfaces;
using PageCounterPro.Core.Models;
using Microsoft.Extensions.Logging;

/// <summary>
/// Service that orchestrates page counting using registered providers.
/// </summary>
public sealed class PageCountService : IPageCountService
{
    private readonly IFileScanner _fileScanner;
    private readonly IEnumerable<IPageCountProvider> _providers;
    private readonly AppSettings _settings;
    private readonly ILogger<PageCountService> _logger;

    public PageCountService(
        IFileScanner fileScanner,
        IEnumerable<IPageCountProvider> providers,
        AppSettings settings,
        ILogger<PageCountService> logger)
    {
        _fileScanner = fileScanner;
        _providers = providers;
        _settings = settings;
        _logger = logger;
    }

    public async Task<ScanResult> ExecuteScanAsync(
        ScanOptions options,
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var result = new ScanResult
        {
            StartTime = DateTime.Now,
            RootFolderPath = options.RootFolderPath
        };

        _logger.LogInformation("Starting scan of {Path}", options.RootFolderPath);

        try
        {
            // Report that we're discovering files
            progress?.Report(new ScanProgress
            {
                IsEnumerating = true,
                TotalCount = 0,
                ProcessedCount = 0,
                StatusMessage = "Discovering files..."
            });

            // Get total file count for progress reporting (with progress updates)
            var totalFiles = await GetTotalFileCountWithProgressAsync(options, progress, cancellationToken);
            result.TotalFilesFound = totalFiles;

            progress?.Report(new ScanProgress
            {
                TotalCount = totalFiles,
                ProcessedCount = 0,
                StatusMessage = $"Found {totalFiles} files. Starting scan..."
            });

            var processedCount = 0;

            await foreach (var file in _fileScanner.ScanFilesAsync(options, progress, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var pageCountResult = GetPageCount(file);

                    var metadata = new FileMetadata
                    {
                        FullPath = file.FullName,
                        RootPath = file.DirectoryName ?? string.Empty,
                        FileName = file.Name,
                        FileSizeBytes = file.Length,
                        FileType = file.Extension.TrimStart('.').ToLowerInvariant(),
                        PageCount = pageCountResult.PageCount,
                        Notes = pageCountResult.Notes,
                        ProcessedSuccessfully = pageCountResult.Success
                    };

                    result.Files.Add(metadata);

                    if (!pageCountResult.Success)
                    {
                        result.FilesWithErrors++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error processing file: {Path}", file.FullName);

                    result.Files.Add(new FileMetadata
                    {
                        FullPath = file.FullName,
                        RootPath = file.DirectoryName ?? string.Empty,
                        FileName = file.Name,
                        FileSizeBytes = TryGetFileSize(file),
                        FileType = file.Extension.TrimStart('.').ToLowerInvariant(),
                        PageCount = null,
                        Notes = $"Error: {ex.Message}",
                        ProcessedSuccessfully = false
                    });

                    result.FilesWithErrors++;
                }

                processedCount++;
                result.FilesProcessed = processedCount;

                progress?.Report(new ScanProgress
                {
                    TotalCount = totalFiles,
                    ProcessedCount = processedCount,
                    CurrentFile = file.FullName,
                    StatusMessage = $"Processing: {file.Name}"
                });
            }

            result.IsComplete = true;
            _logger.LogInformation("Scan completed: {Processed} files processed, {Errors} errors",
                result.FilesProcessed, result.FilesWithErrors);
        }
        catch (OperationCanceledException)
        {
            result.WasCancelled = true;
            _logger.LogInformation("Scan was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scan failed with error");
            throw;
        }
        finally
        {
            result.EndTime = DateTime.Now;
        }

        return result;
    }

    public PageCountResult GetPageCount(FileInfo file)
    {
        var provider = _providers.FirstOrDefault(p => p.CanHandle(file));

        if (provider == null)
        {
            _logger.LogDebug("No provider found for file type: {Extension}", file.Extension);
            return PageCountResult.Unsupported();
        }

        try
        {
            return provider.GetPageCount(file, _settings);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Provider failed for {FileName}", file.Name);
            return PageCountResult.Failed($"Error: provider failed; exception: {ex.Message}");
        }
    }

    private static long TryGetFileSize(FileInfo file)
    {
        try
        {
            return file.Length;
        }
        catch
        {
            return 0;
        }
    }

    private async Task<int> GetTotalFileCountWithProgressAsync(
        ScanOptions options,
        IProgress<ScanProgress>? progress,
        CancellationToken cancellationToken)
    {
        try
        {
            var searchOption = options.IncludeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var directory = new DirectoryInfo(options.RootFolderPath);

            if (!directory.Exists)
            {
                return 0;
            }

            return await Task.Run(() =>
            {
                IEnumerable<FileInfo> allFiles = directory.EnumerateFiles("*", searchOption);

                if (options.MaxDepth.HasValue)
                {
                    var rootDepth = GetPathDepth(options.RootFolderPath);
                    allFiles = allFiles.Where(f => GetPathDepth(f.DirectoryName!) - rootDepth <= options.MaxDepth.Value);
                }

                if (options.FileTypeFilter?.Any() == true)
                {
                    var extensions = options.FileTypeFilter.Select(e => $".{e.ToLowerInvariant()}").ToHashSet();
                    allFiles = allFiles.Where(f => extensions.Contains(f.Extension.ToLowerInvariant()));
                }

                var count = 0;
                foreach (var file in allFiles)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    count++;

                    if (count % 50 == 0)
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
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return 0;
        }
    }

    private static int GetPathDepth(string path)
    {
        return path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Count(s => !string.IsNullOrEmpty(s));
    }
}
