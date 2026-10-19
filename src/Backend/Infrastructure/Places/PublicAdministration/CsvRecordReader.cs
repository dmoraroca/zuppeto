using System.Text;

namespace Zuppeto.Infrastructure.Places.PublicAdministration;

internal static class CsvRecordReader
{
    public static async IAsyncEnumerable<string[]> ReadAsync(
        Stream stream,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, true, 16 * 1024, leaveOpen: true);
        var row = new List<string>();
        var field = new StringBuilder();
        var buffer = new char[1];
        var quoted = false;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var next = await ReadCharacterAsync(reader, buffer, cancellationToken);
            if (next == -1)
            {
                if (quoted) throw new FormatException("El CSV acaba dins d’un camp entre cometes.");
                if (field.Length > 0 || row.Count > 0)
                {
                    row.Add(field.ToString());
                    yield return row.ToArray();
                }
                yield break;
            }

            var character = (char)next;
            if (quoted)
            {
                if (character == '"')
                {
                    if (reader.Peek() == '"')
                    {
                        await ReadCharacterAsync(reader, buffer, cancellationToken);
                        field.Append('"');
                    }
                    else quoted = false;
                }
                else field.Append(character);
                continue;
            }

            if (character == '"' && field.Length == 0) quoted = true;
            else if (character == ',')
            {
                row.Add(field.ToString());
                field.Clear();
            }
            else if (character is '\r' or '\n')
            {
                if (character == '\r' && reader.Peek() == '\n')
                    await ReadCharacterAsync(reader, buffer, cancellationToken);
                row.Add(field.ToString());
                field.Clear();
                yield return row.ToArray();
                row.Clear();
            }
            else field.Append(character);
        }
    }

    private static async ValueTask<int> ReadCharacterAsync(
        StreamReader reader, Memory<char> buffer, CancellationToken cancellationToken)
    {
        var read = await reader.ReadAsync(buffer, cancellationToken);
        return read == 0 ? -1 : buffer.Span[0];
    }
}
