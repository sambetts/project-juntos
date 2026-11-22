namespace Compicasa.Core;

/// <summary>
/// A message for the front end to put into words: a string-table key plus its arguments
/// (numbers, ids, lists of ids, or a nested Msg). Lists of member ids are rendered as names,
/// with the viewer shown as "you" and placed last.
/// </summary>
public sealed record Msg(string Key, params object[] Args)
{
    public override string ToString() => Args.Length == 0 ? Key : $"{Key}({string.Join(", ", Args.Select(Format))})";

    private static string Format(object a) => a switch
    {
        IEnumerable<string> list => "[" + string.Join(",", list) + "]",
        _ => a.ToString() ?? "",
    };
}
