namespace Netptune.Query.Schema;

// Keeps entity-specific value shapes out of the shared schema: a field whose values the parameter
// types cannot describe supplies its own parser.
public interface IQueryValueParser
{
    bool TryParse(string value, out object? parsed);
}
