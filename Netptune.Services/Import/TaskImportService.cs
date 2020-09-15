using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using CsvHelper;

using MoreLinq.Extensions;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Import;
using Netptune.Core.Models.Import;
using Netptune.Core.Relationships;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Import;
using Netptune.Core.UnitOfWork;
using Netptune.Services.Import.Common;

namespace Netptune.Services.Import
{
    public class TaskImportService : ImportService<TaskImportResult>, ITaskImportService
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
            var userId = await IdentityService.GetCurrentUserId();

            using var reader = new StreamReader(stream);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            csv.Configuration.PrepareHeaderForMatch = (header, index) => header.ToLower();

            var headerValidator = new HeaderValidator();

            csv.Configuration.MissingFieldFound = (headerNames, index, context) =>
            {
                headerValidator.AddMissingField(headerNames[index]);
            };

            csv.Configuration.HeaderValidated = (isValid, headerNames, index, context) =>
            {
                headerValidator.ValidateHeaderRow(isValid, headerNames, index);
            };

            var rows = await csv.GetRecordsAsync<TaskImportRow>().ToListAsync();

            var validationResult = headerValidator.GetResult();

            if (!validationResult.IsSuccess)
            {
                return Failed("Import File headers did not match expected fields.", new TaskImportResult
                {
                    HeaderValidationResult = validationResult,
                });
            }

            var groups = rows.Select(row => row.Group.Trim().ToLowerInvariant()).Distinct();
            var board = await UnitOfWork.Boards.GetByIdentifier(boardId, true);

            if (board is null)
            {
                return Failed($"board with identifier '{boardId}' does not exist.");
            }


            var emailAddresses = GetEmailAddresses(rows);
            var users = await UnitOfWork.Users.GetByEmailRange(emailAddresses, true);

            if (users.Count != emailAddresses.Count)
            {
                var existingUserEmails = users.Select(user => user.Email);
                var missingUserEmails = emailAddresses.Except(existingUserEmails);

                return Failed("Import File contained email addresses that do not belong to users in Netptune", new TaskImportResult
                {
                    MissingEmails = missingUserEmails,
                });
            }

            var project = await UnitOfWork.Projects.GetAsync(board.ProjectId, true);
            var workspaceId = project.WorkspaceId;

            var existingGroups = await UnitOfWork.BoardGroups.GetBoardGroupsInBoard(board.Id, true);
            var existingGroupNames = existingGroups.Select(group => group.Name.Trim().ToLowerInvariant()).ToList();
            var nextGroupOrder = existingGroups.MaxBy(group => group.SortOrder).Select(group => group.SortOrder).FirstOrDefault() + 1;

            var newGroups = groups.Where(group => !existingGroupNames.Contains(group));

            var initialScopeId = await UnitOfWork.Tasks.GetNextScopeId(project.Id);

            if (initialScopeId is null)
            {
                return Failed("Failed to generate next project scope Id");
            }

            var tasks = rows.Select(CreateImportTask(project, workspaceId, users, initialScopeId.Value)).ToList();

            var boardGroups = newGroups.Select((group, index) => new BoardGroup
            {
                SortOrder = nextGroupOrder + index,
                OwnerId = userId,
                BoardId = board.Id,
                Name = group,
                Type = BoardGroupType.Basic,
                WorkspaceId = workspaceId,
            }).ToList();

            await UnitOfWork.Transaction(async () =>
            {
                await UnitOfWork.Tasks.AddRangeAsync(tasks);
                await UnitOfWork.BoardGroups.AddRangeAsync(boardGroups);

                await UnitOfWork.CompleteAsync();

                var allBoardGroups = boardGroups.Concat(existingGroups);

                var tasksInGroups = tasks.Select((task, i) =>
                {
                    var boardGroup = allBoardGroups.FirstOrDefault(group => string.Equals(
                            group.Name,
                            rows[i].Group,
                            StringComparison.InvariantCultureIgnoreCase
                        )
                    );

                    if (boardGroup is null)
                    {
                        throw new Exception($"Could not find existing group with name {rows[i].Group}");
                    }

                    return new ProjectTaskInBoardGroup
                    {
                        ProjectTaskId = task.Id,
                        BoardGroupId = boardGroup.Id,
                        SortOrder = task.SortOrder,
                    };
                });

                await UnitOfWork.ProjectTasksInGroups.AddRangeAsync(tasksInGroups);

                await UnitOfWork.CompleteAsync();
            }, true);

            return Success();
        }

        private static Func<TaskImportRow, int, ProjectTask> CreateImportTask(
            Project project, int workspaceId, IEnumerable<AppUser> users, int initialScopeId)
        {
            var userList = users.ToList();

            static DateTime? ParseDateTime(string input)
            {
                var isValid = DateTime.TryParse(input, out var result);

                if  (isValid) return result;

                return null;
            }

            string FindUserId(string email)
            {
                var target = email.Normalize();

                var result = userList.FirstOrDefault(user => string.Equals(
                    user.NormalizedEmail,
                    target,
                    StringComparison.InvariantCultureIgnoreCase
                    )
                );

                return result?.Id;
            }

            static bool ParseBool(string input)
            {
                return string.Equals(input.Trim(), "true", StringComparison.InvariantCultureIgnoreCase);
            }

            static ProjectTaskStatus ParseStatus(string input)
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
                CreatedAt = ParseDateTime(row.CreatedAt) ?? DateTime.UtcNow,
                UpdatedAt = ParseDateTime(row.UpdatedAt),
                SortOrder = double.Parse(row.SortOrder),
                Status = ParseStatus(row.Status),
                AssigneeId = FindUserId(row.AssigneeEmail),
                OwnerId = FindUserId(row.OwnerEmail),
                ProjectScopeId = initialScopeId + i
            };
        }

        private static List<string> GetEmailAddresses(IEnumerable<TaskImportRow> rows)
        {
            return rows
                .Select(row => new { row.AssigneeEmail, row.OwnerEmail })
                .Aggregate(new List<string>(), (result, current) =>
                {
                    if (!string.IsNullOrEmpty(current.OwnerEmail))
                    {
                        result.Add(current.OwnerEmail);
                    }

                    if (!string.IsNullOrEmpty(current.AssigneeEmail))
                    {
                        result.Add(current.AssigneeEmail);
                    }

                    return result;
                })
                .Distinct()
                .ToList();
        }
    }
}
