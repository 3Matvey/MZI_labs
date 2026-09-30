namespace ConsoleUi;

internal sealed class FilePicker(Ui ui)
{
    private string _directory = Environment.CurrentDirectory;

    public PromptResult<string> Pick(string title, string? initialDirectory,
        bool directoryOnly, Func<string, bool>? filter = null)
    {
        if (initialDirectory is not null && !TryNavigate(initialDirectory))
            return PromptResult<string>.Cancelled;
        while (true)
        {
            var selected = ui.Select($"{title}\n{ui.FormatPath(_directory)}", GetEntries(directoryOnly, filter));
            if (selected.IsCancelled) return PromptResult<string>.Cancelled;
            var result = HandleEntry(selected.Value, directoryOnly, filter);
            if (result.HasValue) return result;
        }
    }

    private List<Choice<Entry>> GetEntries(bool directoryOnly, Func<string, bool>? filter)
    {
        var entries = new List<Choice<Entry>>();
        if (directoryOnly) entries.Add(new("Выбрать эту папку", new(EntryKind.Accept, _directory)));
        string? parent = Path.GetDirectoryName(_directory);
        if (parent is not null) entries.Add(new(".. На уровень выше", new(EntryKind.Directory, parent)));
        entries.Add(new("Ввести путь вручную", new(EntryKind.Manual, "")));
        entries.Add(new("Выбрать диск", new(EntryKind.Drives, "")));
        AddDirectoryContents(entries, directoryOnly, filter);
        return entries;
    }

    private void AddDirectoryContents(List<Choice<Entry>> entries, bool directoryOnly, Func<string, bool>? filter)
    {
        try
        {
            entries.AddRange(Directory.GetDirectories(_directory).Order(StringComparer.OrdinalIgnoreCase)
                .Select(path => new Choice<Entry>($"[Папка] {Path.GetFileName(path)}", new(EntryKind.Directory, path))));
            if (!directoryOnly) entries.AddRange(GetFiles(filter));
        }
        catch (Exception exception) when (FilePaths.IsPathError(exception))
        {
            ui.ShowError($"Не удалось прочитать папку: {exception.Message}");
        }
    }

    private IEnumerable<Choice<Entry>> GetFiles(Func<string, bool>? filter) =>
        Directory.GetFiles(_directory).Where(path => filter?.Invoke(path) ?? true)
            .Order(StringComparer.OrdinalIgnoreCase)
            .Select(path => new Choice<Entry>(Path.GetFileName(path), new(EntryKind.File, path)));

    private PromptResult<string> HandleEntry(Entry entry, bool directoryOnly, Func<string, bool>? filter)
    {
        switch (entry.Kind)
        {
            case EntryKind.File: return AcceptFile(entry.Path, filter);
            case EntryKind.Accept: return AcceptDirectory();
            case EntryKind.Directory: TryNavigate(entry.Path); break;
            case EntryKind.Manual: return ReadPath(directoryOnly, filter);
            case EntryKind.Drives: SelectDrive(); break;
        }
        return PromptResult<string>.Cancelled;
    }

    private bool TryNavigate(string path)
    {
        try
        {
            string resolved = FilePaths.Resolve(path, _directory);
            _ = Directory.GetFileSystemEntries(resolved);
            _directory = Path.TrimEndingDirectorySeparator(resolved);
            return true;
        }
        catch (Exception exception) when (FilePaths.IsPathError(exception))
        {
            ui.ShowError($"Не удалось открыть папку: {exception.Message}");
            return false;
        }
    }

    private PromptResult<string> ReadPath(bool directoryOnly, Func<string, bool>? filter)
    {
        var input = ui.ReadText("Полный или относительный путь");
        if (input.IsCancelled) return PromptResult<string>.Cancelled;
        try
        {
            string path = FilePaths.Resolve(input.Value, _directory);
            if (!directoryOnly && File.Exists(path)) return AcceptFile(path, filter);
            TryNavigate(path);
        }
        catch (Exception exception) when (FilePaths.IsPathError(exception)) { ui.ShowError(exception.Message); }
        return PromptResult<string>.Cancelled;
    }

    private PromptResult<string> AcceptFile(string path, Func<string, bool>? filter)
    {
        if (!File.Exists(path) || !(filter?.Invoke(path) ?? true))
        {
            ui.ShowError("Файл недоступен или не подходит под фильтр.");
            return PromptResult<string>.Cancelled;
        }
        _directory = Path.GetDirectoryName(path)!;
        return PromptResult<string>.Success(path);
    }

    private PromptResult<string> AcceptDirectory()
    {
        return TryNavigate(_directory)
            ? PromptResult<string>.Success(_directory) : PromptResult<string>.Cancelled;
    }

    private void SelectDrive()
    {
        var drives = DriveInfo.GetDrives().Select(drive => new Choice<string>(drive.Name, drive.Name)).ToArray();
        if (drives.Length == 0) { ui.ShowError("Нет доступных дисков."); return; }
        var selected = ui.Select("Выберите диск", drives);
        if (selected.HasValue) TryNavigate(selected.Value);
    }

    private enum EntryKind { Directory, File, Accept, Manual, Drives }
    private sealed record Entry(EntryKind Kind, string Path);
}
