using ConsoleUi;
using Spectre.Console;

var suite = new UiTests();
suite.Run();

internal sealed class UiTests
{
    private int _passed;
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "ConsoleUi-tests-" + Guid.NewGuid());

    public void Run()
    {
        Directory.CreateDirectory(_directory);
        try
        {
            CheckPrompts();
            CheckEscape();
            CheckFiles();
            CheckSavePath();
            Console.WriteLine($"PASS: {_passed} checks");
        }
        finally { Directory.Delete(_directory, recursive: true); }
    }

    private static Ui Create(string input, StringWriter? output = null)
    {
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Out = new AnsiConsoleOutput(output ?? new StringWriter()),
            Ansi = AnsiSupport.No,
            Interactive = InteractionSupport.No
        });
        return new Ui(console, new StringReader(input));
    }

    private void CheckPrompts()
    {
        Choice<int>[] choices = [new("zero", 0), new("ten", 10)];
        Check(Create("bad\n3\n2\n").Select("select", choices).Value == 10, "retry selection");
        Check(Create("1\n").Select("select", choices).Value == 0, "default value is not cancellation");
        Check(Create("0\n").Select("select", choices).IsCancelled, "cancel selection");
        Check(Create("").Select("select", choices).IsCancelled, "EOF selection");
        Check(Create("\n").ReadText("text", "default").Value == "default", "default text");
        Check(Create("\na\nok\n").ReadText("text", validate: s => s.Length < 2 ? "short" : null).Value == "ok", "validation");
        Check(Create("/cancel\n").ReadText("text").IsCancelled, "cancel text");
        Check(Create("").ReadText("text").IsCancelled, "EOF text");
        CheckNumbers();
        CheckMessages();
    }

    private void CheckEscape()
    {
        var ui = new Ui(new KeyboardConsole("draft\u001bok\r"));
        Check(ui.ReadText("text", "default").IsCancelled, "Esc discards partial text and default");
        Check(ui.ReadText("next").Value == "ok", "next prompt works after Esc");
        Check(new Ui(new KeyboardConsole("\u001b")).ReadText("text").IsCancelled, "Esc on empty input");
        Check(new Ui(new KeyboardConsole("12\u001b")).ReadNumber<int>("number").IsCancelled, "Esc on number");
        Check(new Ui(new KeyboardConsole("bad\r\u001b")).ReadNumber<int>("number").IsCancelled, "Esc after validation error");
        Check(new Ui(new KeyboardConsole("/cancel\r")).ReadText("text").Value == "/cancel", "literal cancel text");
        Check(new Ui(new KeyboardConsole("ab\bc\r")).ReadText("text").Value == "ac", "backspace preserved");
        Check(new Ui(new KeyboardConsole("\r")).ReadText("text", "default").Value == "default", "Enter default preserved");
        Check(new Ui(new KeyboardConsole("")).ReadText("text").IsCancelled, "interactive EOF");
    }

    private void CheckNumbers()
    {
        Check(Create("abc\n999999999999999\n-1\n5\n").ReadNumber<int>("number",
            validate: n => n > 0 ? null : "positive").Value == 5, "number retries and overflow");
        Check(Create("\n").ReadNumber<int>("number", 10).Value == 10, "default number");
        Check(Create("/cancel\n").ReadNumber<int>("number").IsCancelled, "cancel number");
        Check(Create("NaN\nInfinity\n2\n").ReadNumber<double>("number").Value == 2, "finite numbers only");
        Check(Create("1\n").Confirm("confirm").Value, "yes");
        Check(!Create("2\n").Confirm("confirm").Value, "no");
        Check(Create("0\n").Confirm("confirm").IsCancelled, "cancel is distinct from no");
    }

    private void CheckMessages()
    {
        var output = new StringWriter();
        var ui = Create("", output);
        ui.ShowSuccess("[red]literal[/]");
        ui.ShowError("[blue]literal[/]");
        Check(output.ToString().Contains("[red]literal[/]"), "success escapes markup");
        Check(output.ToString().Contains("[blue]literal[/]"), "error escapes markup");
    }

    private void CheckFiles()
    {
        string file = Path.Combine(_directory, "[пример].txt");
        File.WriteAllText(file, "test");
        File.WriteAllText(Path.Combine(_directory, "hidden.bin"), "test");
        Check(Create("4\n").PickFile("file", _directory, p => p.EndsWith(".txt")).Value == file, "filtered file list");
        Check(Create($"2\n\"{file}\"\n").PickFile("file", _directory).Value == file, "quoted manual path");
        Check(Create("2\nhidden.bin\n0\n").PickFile("file", _directory, p => p.EndsWith(".txt")).IsCancelled, "manual path obeys filter");
        Check(Create("1\n").PickDirectory("folder", _directory).Value == _directory, "directory selection");
        Check(Create("0\n").PickFile("file", _directory).IsCancelled, "file cancellation");
        Check(Create("").PickFile("file", _directory).IsCancelled, "file EOF");
        Check(Create("").PickDirectory("folder", Path.Combine(_directory, "missing")).IsCancelled, "missing folder");
        CheckFolderMemory();
    }

    private void CheckFolderMemory()
    {
        var ui = Create("1\n1\n");
        ui.PickDirectory("first", _directory);
        Check(ui.PickDirectory("second").Value == _directory, "remember folder");
        string subdirectory = Path.Combine(_directory, "nested");
        Directory.CreateDirectory(subdirectory);
        Check(Create("5\n1\n").PickDirectory("nested", _directory).Value == subdirectory, "enter subdirectory");
        Check(Create("2\n1\n").PickDirectory("parent", subdirectory).Value == _directory, "navigate up");
    }

    private void CheckSavePath()
    {
        string expected = Path.Combine(_directory, "new.bin");
        Check(Create("1\n\n").PickSavePath("save", "new.bin", _directory).Value == expected, "default filename");
        Check(!File.Exists(expected), "picker does not create files");
        Check(Create("1\n[пример].txt\n../wrong\nnew.bin\n").PickSavePath("save", "key.bin", _directory).Value == expected,
            "reject overwrite and traversal");
        Check(Create("1\n/cancel\n").PickSavePath("save", "new.bin", _directory).IsCancelled, "cancel filename");
        Check(Create("0\n").PickSavePath("save", "new.bin", _directory).IsCancelled, "cancel save directory");
    }

    private void Check(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException("FAIL: " + description);
        _passed++;
    }
}
