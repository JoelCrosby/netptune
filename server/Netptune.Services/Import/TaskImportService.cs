using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using CsvHelper;
using CsvHelper.Configuration;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Extensions;
using Netptune.Core.Import;
using Netptune.Core.Models.Import;
using Netptune.Core.Relationships;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Common;
using Netptune.Core.Services.Import;
using Netptune.Core.UnitOfWork;

namespace Netptune.Services.Import;

public class TaskImportService : ServiceBase<TaskImportResult>, ITaskImportService
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService IdentityService;

    public TaskImportService(INetptuneUnitOfWork unitOfWork, IIdentityService identityService)
    {
        UnitOfWork = unitOfWork;
        IdentityService = identityService;
    }

    public async Task<ClientResponse<TaskImportResult>> ImportWorkspaceTasks(string boardId, Stream stream)
    {
        var userId = IdentityService.GetCurrentUserId();

        var headerValidator = new HeaderValidator();

        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            PrepareHeaderForMatch = args => args.Header.ToLower(),
            MissingFieldFound = args =>
            {
                if (args.HeaderNames is not null)
                {
                    headerValidator.AddMissingField(args.HeaderNames[args.Index]);
                }
            },
            HeaderValidated = args =>
            {
                headerValidator.ValidateHeaderRow(args.InvalidHeaders);
            },
        });

        csv.AddDateFormatting();
        csv.Context.RegisterClassMap<TaskImportRowMap>();

        // Read csv rows into PCO
        var rows = await csv.GetRecordsAsync<TaskImportRow>().ToListAsync();

        var validationResult = headerValidator.GetResult();

        if (!validationResult.IsSuccess)
        {
            return Failed("Import File headers did not match expected fields.", new TaskImportResult
            {
                HeaderValidationResult = validationResult,
            });
        }

        var groups = rows.Select(row => row.Group?.Trim().ToLowerInvariant()).Distinct();
        var workspaceId = await IdentityService.GetWorkspaceId();
        var board = await UnitOfWork.Boards.GetByIdentifier(boardId, workspaceId, true);

        if (board is null)
        {
            return Failed($"board with identifier '{boardId}' does not exist.");
        }

        var emailAddresses = GetEmailAddresses(rows);
        var users = await UnitOfWork.Users.GetByEmailRange(emailAddresses, true);

        if (users.Count != emailAddresses.Count)
        {
            var existingUserEmails = users.Select(user => user.NormalizedEmail!);
            var missingUserEmails = emailAddresses.Except(existingUserEmails);

            return Failed("Import File contained email addresses that do not belong to users in Netptune", new TaskImportResult
            {
                MissingEmails = missingUserEmails.Select(email => email.ToLowerInvariant()),
            });
        }

        var project = await UnitOfWork.Projects.GetAsync(board.ProjectId, true);

        if (project is null)
        {
            return Failed($"project with id '{board.ProjectId}' does not exist.");
        }

        var existingGroups = await UnitOfWork.BoardGroups.GetBoardGroupsInBoard(board.Id, true);
        var existingGroupNames = existingGroups.ConvertAll(group => group.Name.Trim().ToLowerInvariant());
        var nextGroupOrder = existingGroups.MaxBy(group => group.SortOrder)?.SortOrder + 1 ?? 0;

        var newGroups = groups.Where(group => !string.IsNullOrEmpty(group) && !existingGroupNames.Contains(group));

        var initialScopeId = await UnitOfWork.Tasks.GetNextScopeId(project.Id);

        if (initialScopeId is null)
        {
            return Failed("failed to generate next project scope Id");
        }

        var tags = ParseUniqueTags(rows);
        var existingTags = await UnitOfWork.Tags.GetTagsInWorkspace(workspaceId, true);
        var existingTagValues = existingTags.Select(tag => tag.Name);
        var newTags = tags.Where(tag => !existingTagValues.Contains(tag)).Select(tag => new Tag
        {
            Name = tag,
            WorkspaceId = workspaceId,
            OwnerId = userId,
        }).ToList();

        var allTags = newTags.Concat(existingTags).ToDictionary(tag => tag.Name, tag => tag);

        var tasks = rows
            .Select(CreateImportTask(project, workspaceId, users, initialScopeId.Value))
            .ToList();

        var boardGroups = newGroups.Select((group, index) => new BoardGroup
        {
            SortOrder = nextGroupOrder + index,
            OwnerId = userId,
            BoardId = board.Id,
            Name = group!,
            Type = BoardGroupType.Basic,
            WorkspaceId = workspaceId,
        }).ToList();

        await UnitOfWork.Transaction(async () =>
        {
            await UnitOfWork.Tasks.AddRangeAsync(tasks);
            await UnitOfWork.BoardGroups.AddRangeAsync(boardGroups);
            await UnitOfWork.Tags.AddRangeAsync(newTags);

            await UnitOfWork.CompleteAsync();

            var orderDict = existingGroups.Select(group => new
                {
                    GroupId = group.Id,
                    BaseOrder = group.TasksInGroups.MaxBy(task => task.SortOrder)?.SortOrder,
                })
                .ToDictionary(group => group.GroupId, group => group.BaseOrder);

            var allBoardGroups = boardGroups.Concat(existingGroups).ToList();
            var groupTaskCounter = allBoardGroups.ToDictionary(group => group.Id, _ => 0);

            var tasksInGroups = tasks.Select((task, i) =>
            {
                var boardGroup = allBoardGroups.Find(group => string.Equals(
                        group.Name,
                        rows[i].Group,
                        StringComparison.InvariantCultureIgnoreCase
                    )
                );

                if (boardGroup is null)
                {
                    throw new Exception($"Could not find existing group with name {rows[i].Group}");
                }

                var hasBaseOrder = orderDict.TryGetValue(boardGroup.Id, out var order);
                var baseOrder = hasBaseOrder && order.HasValue ? order.Value : 0;

                groupTaskCounter[boardGroup.Id]++;

                return new ProjectTaskInBoardGroup
                {
                    ProjectTaskId = task.Id,
                    BoardGroupId = boardGroup.Id,
                    SortOrder = baseOrder + groupTaskCounter[boardGroup.Id],
                };
            });

            IEnumerable<Tag> ParseTags(string? tagRow)
            {
                var names = ParseTagString(tagRow);
                return names.Select(name => allTags[name]).ToList();
            }

            var taskTagIndex = -1;
            var taskTags = tasks.Aggregate(new List<ProjectTaskTag>(), (acc, task) =>
            {
                taskTagIndex ++;

                var tagString = rows.ElementAtOrDefault(taskTagIndex)?.Tags;

                if (tagString is null || string.IsNullOrWhiteSpace(tagString))
                {
                    return acc;
                }

                var aggregateTags = ParseTags(rows[taskTagIndex]?.Tags);
                var newTaskTags = aggregateTags.Select(tag => new ProjectTaskTag
                {
                    ProjectTaskId = task.Id,
                    TagId = tag.Id,
                });

                return acc.Concat(newTaskTags).ToList();
            });

            await UnitOfWork.ProjectTasksInGroups.AddRangeAsync(tasksInGroups);
            await UnitOfWork.ProjectTaskTags.AddRangeAsync(taskTags);

            await UnitOfWork.CompleteAsync();
        }, true);

        return Success();
    }

    private static Func<TaskImportRow, int, ProjectTask> CreateImportTask(
        Project project, int workspaceId, IEnumerable<AppUser> users, int initialScopeId)
    {
        var userList = users.ToList();

        static bool ParseBool(string? input)
        {
            return string.Equals(input?.Trim(), "true", StringComparison.InvariantCultureIgnoreCase);
        }

        static ProjectTaskStatus ParseStatus(string? input)
        {
            var isValid = Enum.TryParse(typeof(ProjectTaskStatus), input, true, out var status);

            return isValid && status is {} ? (ProjectTaskStatus)status : ProjectTaskStatus.New;
        }

        return (row, i) => new ProjectTask
        {
            Name = row.Name,
            Description = row.Description,
            WorkspaceId = workspaceId,
            ProjectId = project.Id,
            IsFlagged = ParseBool(row.IsFlagged),
            CreatedAt = row.CreatedAt,
            UpdatedAt = row.UpdatedAt,
            Status = ParseStatus(row.Status),
            OwnerId = FindUserId(userList, row.Owner),
            ProjectScopeId = initialScopeId + i,
            ProjectTaskAppUsers = GetUsersFromArrayCell(userList, row.Assignees),
        };
    }

    private static string? FindUserId(List<AppUser> users, string? email)
    {
        var target = email?.IdentityNormalize();

        var result = users.Find(user => string.Equals(
                user.NormalizedEmail,
                target,
                StringComparison.InvariantCultureIgnoreCase
            )
        );

        return result?.Id;
    }

    private static List<ProjectTaskAppUser> GetUsersFromArrayCell(List<AppUser> users, string? cell)
    {
        var items = ParseCellArray(cell);
        var results = new List<ProjectTaskAppUser>();

        foreach (var item in items)
        {
            var userId = FindUserId(users, item);
            if (userId is null) continue;

            results.Add(new ProjectTaskAppUser
            {
                UserId = userId,
            });
        }

        return results;
    }

    private static List<string> GetEmailAddresses(IEnumerable<TaskImportRow> rows)
    {
        return rows
            .Aggregate(new HashSet<string>(), (result, current) =>
            {
                foreach (var email in ParseCellArray(current.Assignees))
                {
                    result.Add(email.Trim().IdentityNormalize());
                }

                return result;
            })
            .ToList();
    }

    private static List<string> ParseCellArray(string? cell)
    {
        if (string.IsNullOrWhiteSpace(cell)) return new List<string>();

        return cell.Split('|', StringSplitOptions.RemoveEmptyEntries).ToHashSet().ToList();
    }

    private static IEnumerable<string> ParseUniqueTags(IEnumerable<TaskImportRow> rows)
    {
        return rows.Select(row => row.Tags).Select(ParseTagString).Aggregate(new HashSet<string>(), (prev, tags) =>
        {
            foreach (var tag in tags)
            {
                prev.Add(tag);
            }

            return prev;
        });
    }

    private static IEnumerable<string> ParseTagString(string? tagString)
    {
        if (string.IsNullOrWhiteSpace(tagString))
        {
            return Array.Empty<string>();
        }

        if (!tagString.Contains('|'))
        {
            return new[] { tagString.Trim() };
        }

        return tagString
            .Split('|', StringSplitOptions.RemoveEmptyEntries)
            .Select(tag => tag.Trim())
            .ToHashSet();
    }
}
