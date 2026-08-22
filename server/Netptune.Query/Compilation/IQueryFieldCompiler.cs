namespace Netptune.Query.Compilation;

public interface IQueryFieldCompiler
{
    string Compile(QueryCompileRequest request, QueryParameterBag parameters);
}
