namespace Compicasa.Core;

/// <summary>
/// A message for the front end to put into words: a string-table key plus its arguments
/// (numbers, ids, lists of ids, or a nested Msg). Lists of member ids are rendered as names,
/// with the viewer shown as "you" and placed last.
/// </summary>
public sealed record Msg
{
    public string Key { get; }
    public object[] Args { get; }

    public Msg(string key, params object[] args)
    {
        Key = key;
        // C# passes a lone string[] as the params array itself (array covariance), which would spread
        // one list argument into several. Keep it as one argument.
        Args = args.GetType() == typeof(object[]) ? args : [args];
    }

    public override string ToString() => Args.Length == 0 ? Key : $"{Key}({string.Join(", ", Args.Select(Format))})";

    private static string Format(object a) => a switch
    {
        IEnumerable<string> list => "[" + string.Join(",", list) + "]",
        _ => a.ToString() ?? "",
    };
}
