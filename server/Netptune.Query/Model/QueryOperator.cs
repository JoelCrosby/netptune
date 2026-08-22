namespace Netptune.Query.Model;

public enum QueryOperator
{
    Equals = 0,
    NotEquals = 1,
    In = 2,
    NotIn = 3,
    Contains = 4,
    NotContains = 5,
    StartsWith = 6,
    IsEmpty = 7,
    IsNotEmpty = 8,
    GreaterThan = 9,
    GreaterThanOrEqual = 10,
    LessThan = 11,
    LessThanOrEqual = 12,
    Between = 13,
    InNextDays = 14,
    InLastDays = 15,
    IsOverdue = 16,
}
