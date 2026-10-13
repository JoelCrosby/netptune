using System.Linq.Expressions;
using System.Text.RegularExpressions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

using Netptune.Core.Exceptions;
using Netptune.Core.Models.Search;

using Npgsql;

namespace Netptune.Repositories.Common;

public static class TextMatch
{
    private const string LikeEscape = @"\";

    private static readonly TimeSpan PatternTimeout = TimeSpan.FromSeconds(10);

    public static Expression<Func<T, bool>> On<T>(Expression<Func<T, string?>> selector, TextPattern pattern)
    {
        var textPredicate = ForText(pattern);
        var body = ReplacingExpressionVisitor.Replace(textPredicate.Parameters[0], selector.Body, textPredicate.Body);

        return Expression.Lambda<Func<T, bool>>(body, selector.Parameters);
    }

    public static Expression<Func<T, bool>> AnyOf<T>(IReadOnlyList<Expression<Func<T, bool>>> predicates)
    {
        var parameter = predicates[0].Parameters[0];
        var bodies = predicates.Select(predicate =>
            ReplacingExpressionVisitor.Replace(predicate.Parameters[0], parameter, predicate.Body));
        var combined = bodies.Aggregate(Expression.OrElse);

        return Expression.Lambda<Func<T, bool>>(combined, parameter);
    }

    public static async Task<TResult> RunPatternQuery<TResult>(DbContext context, Func<Task<TResult>> query)
    {
        var previousTimeout = context.Database.GetCommandTimeout();

        context.Database.SetCommandTimeout(PatternTimeout);

        try
        {
            return await query();
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.InvalidRegularExpression)
        {
            throw new InvalidTextPatternException($"The pattern is not a valid regular expression: {exception.MessageText}");
        }
        catch (NpgsqlException exception) when (exception.InnerException is TimeoutException)
        {
            throw new InvalidTextPatternException("The pattern took too long to run. Try a simpler or more specific pattern.");
        }
        finally
        {
            context.Database.SetCommandTimeout(previousTimeout);
        }
    }

    private static Expression<Func<string?, bool>> ForText(TextPattern pattern)
    {
        var expression = pattern.Pattern;
        var isCaseSensitiveRegex = pattern.IsRegex && pattern.IsCaseSensitive;

        // Npgsql only translates constant options, so each combination needs its own lambda.
        if (isCaseSensitiveRegex)
        {
            return text => Regex.IsMatch(text!, expression, RegexOptions.Multiline);
        }

        if (pattern.IsRegex)
        {
            return text => Regex.IsMatch(text!, expression, RegexOptions.Multiline | RegexOptions.IgnoreCase);
        }

        var like = $"%{EscapeLike(pattern.Pattern)}%";

        if (pattern.IsCaseSensitive)
        {
            return text => EF.Functions.Like(text!, like, LikeEscape);
        }

        return text => EF.Functions.ILike(text!, like, LikeEscape);
    }

    private static string EscapeLike(string value)
    {
        return value
            .Replace(@"\", @"\\")
            .Replace("%", @"\%")
            .Replace("_", @"\_");
    }
}
