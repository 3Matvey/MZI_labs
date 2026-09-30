using Spectre.Console;
using Spectre.Console.Rendering;

internal sealed class KeyboardConsole(string keys) : IAnsiConsole
{
    private readonly IAnsiConsole _console = AnsiConsole.Create(new AnsiConsoleSettings
    {
        Out = new AnsiConsoleOutput(new StringWriter()),
        Ansi = AnsiSupport.No,
        Interactive = InteractionSupport.Yes
    });

    public Profile Profile => _console.Profile;
    public IAnsiConsoleCursor Cursor => _console.Cursor;
    public IAnsiConsoleInput Input { get; } = new KeyboardInput(keys);
    public IExclusivityMode ExclusivityMode => _console.ExclusivityMode;
    public RenderPipeline Pipeline => _console.Pipeline;
    public void Clear(bool home) => _console.Clear(home);
    public void Write(IRenderable renderable) => _console.Write(renderable);
    public void WriteAnsi(Action<AnsiWriter> action) => _console.WriteAnsi(action);

    private sealed class KeyboardInput(string keys) : IAnsiConsoleInput
    {
        private readonly Queue<char> _keys = new(keys);
        public bool IsKeyAvailable() => _keys.Count > 0;

        public ConsoleKeyInfo? ReadKey(bool intercept)
        {
            if (!_keys.TryDequeue(out char character)) return null;
            var key = character switch
            {
                '\u001b' => ConsoleKey.Escape,
                '\r' => ConsoleKey.Enter,
                '\b' => ConsoleKey.Backspace,
                _ => ConsoleKey.A
            };
            return new ConsoleKeyInfo(character, key, false, false, false);
        }

        public Task<ConsoleKeyInfo?> ReadKeyAsync(bool intercept, CancellationToken cancellationToken) =>
            Task.FromResult(ReadKey(intercept));
    }
}
