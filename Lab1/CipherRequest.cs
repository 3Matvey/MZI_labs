namespace MziLab;

internal sealed record CipherRequest(
    string Action, string Mode, string Input, string Output, string KeyPath);
