using Spectre.Console;
using Spectre.Console.Rendering;

namespace ConsoleUi;

// Replace only the prompt's input; rendering and Spectre's input lock stay unchanged.
internal sealed class EscapablePromptConsole(IAnsiConsole console) : IAnsiConsole
{
    public Profile Profile => console.Profile;
    public IAnsiConsoleCursor Cursor => console.Cursor;
    public IAnsiConsoleInput Input { get; } = new EscapeInput(console.Input);
    public IExclusivityMode ExclusivityMode => console.ExclusivityMode;
    public RenderPipeline Pipeline => console.Pipeline;
    public void Clear(bool home) => console.Clear(home);
    public void Write(IRenderable renderable) => console.Write(renderable);
    public void WriteAnsi(Action<AnsiWriter> action) => console.WriteAnsi(action);

    internal sealed class PromptCancelledException : Exception;

    private sealed class EscapeInput(IAnsiConsoleInput input) : IAnsiConsoleInput
    {
        public bool IsKeyAvailable() => input.IsKeyAvailable();
        public ConsoleKeyInfo? ReadKey(bool intercept) => Check(input.ReadKey(intercept));

        public async Task<ConsoleKeyInfo?> ReadKeyAsync(bool intercept, CancellationToken cancellationToken) =>
            Check(await input.ReadKeyAsync(intercept, cancellationToken).ConfigureAwait(false));

        private static ConsoleKeyInfo? Check(ConsoleKeyInfo? key)
        {
            if (key is null || key.Value.Key == ConsoleKey.Escape)
            {
                throw new PromptCancelledException();
            }
            return key;
        }
    }
}
