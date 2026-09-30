namespace ConsoleUi;

internal static class FilePaths
{
    public static bool IsPathError(Exception exception) => exception is
        IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException;

    public static string Resolve(string path, string currentDirectory) =>
        Path.GetFullPath(path.Trim().Trim('"'), currentDirectory);

    public static string? ValidateNewFile(string directory, string name)
    {
        if (name is "." or ".." || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            return "Введите имя файла без пути и недопустимых символов.";
        if (OperatingSystem.IsWindows() && IsInvalidWindowsName(name))
            return "Это имя зарезервировано Windows или заканчивается точкой/пробелом.";
        if (!Directory.Exists(directory)) return "Папка больше не существует. Отмените ввод и выберите другую.";
        string path = Path.Combine(directory, name);
        return File.Exists(path) || Directory.Exists(path) ? "Имя уже занято. Введите другое." : null;
    }

    private static bool IsInvalidWindowsName(string name)
    {
        string stem = name.Split('.')[0].ToUpperInvariant();
        return name.EndsWith('.') || name.EndsWith(' ') || stem is "CON" or "PRN" or "AUX" or "NUL"
            || (stem.Length == 4 && (stem.StartsWith("COM") || stem.StartsWith("LPT"))
                && "123456789¹²³".Contains(stem[3]));
    }
}
