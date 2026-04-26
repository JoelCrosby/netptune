# Unit Test Migration Plan: Service Classes → Mediator Handlers

## Context

The `Netptune.Services` project was migrated from monolithic service classes to individual
Mediator command/query handlers. The unit test project still references the deleted service
classes, causing 12 build errors. This plan describes how to fix each broken file.

## Build Errors (Current)

| File | Error |
|------|-------|
| `AuthenticationServiceTests.cs` | `IWorkspaceService` not found |
| `ActivityServiceTests.cs` | `ActivityService` not found |
| `BoardServiceTests.cs` | `BoardService` not found |
| `BoardGroupServiceTests.cs` | `BoardGroupService` not found |
| `CommentServiceTests.cs` | `CommentService` not found |
| `NotificationServiceTests.cs` | `NotificationService` not found |
| `ProjectServiceTests.cs` | `ProjectService` not found |
| `TagServiceTests.cs` | `TagService` not found |
| `TaskServiceTests.cs` | `TaskService` not found |
| `UserServiceTests.cs` | `UserService` not found |
| `WorkspaceServiceTests.cs` | `WorkspaceService` not found |

## Migration Pattern

**Old pattern** (testing a service class):
```csharp
private readonly BoardService Service;
Service = new(UnitOfWork, Identity, Activity);
var result = await Service.GetBoard(1);
```

**New pattern** (testing a handler directly):
```csharp
private readonly GetBoardQueryHandler Handler;
Handler = new(UnitOfWork);
var result = await Handler.Handle(new GetBoardQuery(1), CancellationToken.None);
```

Key differences:
- Instantiate the specific handler class instead of a broad service
- Wrap the request in the corresponding command/query record
- Pass `CancellationToken.None` as the second `Handle` argument
- Some handlers have fewer constructor dependencies than the old service
- Some return types change: unwrapped `T?` instead of `ClientResponse<T>` for simple queries

## Return Type Changes to Watch

Several handlers return raw types instead of `ClientResponse<T>`:

| Handler | Return type | Old service return type |
|---------|-------------|------------------------|
| `GetBoardQueryHandler` | `ClientResponse<BoardViewModel>` | same |
| `GetBoardsInWorkspaceQueryHandler` | `List<BoardsViewModel>?` | `List<BoardsViewModel>?` |
| `GetBoardsInProjectQueryHandler` | `List<BoardViewModel>?` | `List<Board>` |
| `GetBoardGroupQueryHandler` | `BoardGroup?` | `BoardGroup?` |
| `GetWorkspaceQueryHandler` | `Workspace?` | `Workspace?` |
| `GetUserWorkspacesQueryHandler` | `List<Workspace>` | same |
| `GetAllWorkspacesQueryHandler` | `List<Workspace>` | same |
| `GetWorkspaceUsersQueryHandler` | `List<WorkspaceUserViewModel>` | same |
| `GetUserQueryHandler` | `UserViewModel?` | `UserViewModel?` |
| `GetUserByEmailQueryHandler` | `UserViewModel?` | `UserViewModel?` |
| `GetAllUsersQueryHandler` | unknown — check handler | |

For handlers returning raw types, replace `result.IsSuccess.Should().BeTrue()` with
`result.Should().NotBeNull()`.

---

## File-by-File Plan

### 1. `AuthenticationServiceTests.cs` — Minor fix only

The `NetptuneAuthService` class still exists. Only change: replace `IWorkspaceService` with
`IMediator`.

**Changes:**
1. Remove `using Netptune.Core.Services;` (line 17, only needed for `IWorkspaceService`)
2. Add `using Mediator;`
3. Change field: `IWorkspaceService WorkspaceService = Substitute.For<IWorkspaceService>();`
   → `IMediator Mediator = Substitute.For<IMediator>();`
4. Update constructor call — remove `WorkspaceService` parameter, add `Mediator`

The constructor signature of `NetptuneAuthService` has `IMediator` in place of
`IWorkspaceService`; verify the exact parameter order before updating.

No test logic changes needed — none of the 30 tests call workspace service methods.

---

### 2. `ActivityServiceTests.cs` → Replace with `GetActivitiesQueryHandlerTests.cs`

**Old subject:** `ActivityService(INetptuneUnitOfWork, IIdentityService)`
**New subject:** `GetActivitiesQueryHandler(INetptuneUnitOfWork, IIdentityService)`
Location: `Netptune.Services.Activity.Queries`

**Changes:**
- Class name: `ActivityServiceTests` → `GetActivitiesQueryHandlerTests`
- Field: `ActivityService Service` → `GetActivitiesQueryHandler Handler`
- Constructor: `Service = new(UnitOfWork, Identity)` → `Handler = new(UnitOfWork, Identity)`
- Remove `using Netptune.Services;`
- Add `using Netptune.Services.Activity.Queries;`
- Call site: `await Service.GetActivities(EntityType.Task, 1)`
  → `await Handler.Handle(new GetActivitiesQuery(EntityType.Task, 1), CancellationToken.None)`
- Return type is `ClientResponse<List<ActivityViewModel>>`, so `result.IsSuccess` checks remain

**Tests to preserve:** both tests (`GetActivities_ShouldReturnCorrectly_WhenValidId`,
`GetActivities_ShouldReturnActivities_WithUserAvatars`)

---

### 3. `BoardServiceTests.cs` → Split into per-handler test classes

Each test class goes in `Netptune.UnitTests/Netptune.Services/Boards/`.

#### 3a. `GetBoardQueryHandlerTests.cs`
**Handler:** `GetBoardQueryHandler(INetptuneUnitOfWork)`
**Namespace:** `Netptune.Services.Boards.Queries`
**Tests:** `GetBoard_ShouldReturnCorrectly_WhenInputValid`, `GetBoard_ShouldReturnFailure_WhenNotFound`
**Call:** `Handler.Handle(new GetBoardQuery(1), ct)`

#### 3b. `GetBoardViewQueryHandlerTests.cs`
**Handler:** `GetBoardViewQueryHandler(INetptuneUnitOfWork, IIdentityService)`
**Tests:** 4 tests (success + 3 failure cases)
**Call:** `Handler.Handle(new GetBoardViewQuery(identifier, filter), ct)`
**Note:** `Identity.GetWorkspaceId()` is now async — the handler uses `await Identity.GetWorkspaceId()`; ensure substitute uses `.Returns(workspaceId)`.

#### 3c. `UpdateBoardCommandHandlerTests.cs`
**Handler:** `UpdateBoardCommandHandler(INetptuneUnitOfWork, IActivityLogger)`
**Note:** No `IIdentityService` needed
**Tests:** `Update_ShouldReturnCorrectly`, `Update_ShouldCallCompleteAsync`, `Update_ShouldReturnFailure_WhenNotFound`
**Call:** `Handler.Handle(new UpdateBoardCommand(request), ct)`

#### 3d. `CreateBoardCommandHandlerTests.cs`
**Handler:** `CreateBoardCommandHandler(INetptuneUnitOfWork, IActivityLogger)`
**Note:** No `IIdentityService` needed
**Tests:** `Create_ShouldReturnCorrectly`, `Create_CallCompleteAsync`, `Create_ShouldReturnFailure_WhenProjectNotFound`
**Call:** `Handler.Handle(new CreateBoardCommand(request), ct)`

#### 3e. `DeleteBoardCommandHandlerTests.cs`
**Handler:** `DeleteBoardCommandHandler(INetptuneUnitOfWork, IIdentityService, IActivityLogger)`
**Tests:** 5 tests (success, CompleteAsync called/not called, DeletePermanent not called)
**Call:** `Handler.Handle(new DeleteBoardCommand(1), ct)`

#### 3f. `GetBoardsInWorkspaceQueryHandlerTests.cs`
**Handler:** `GetBoardsInWorkspaceQueryHandler(INetptuneUnitOfWork, IIdentityService)`
**Return type:** `List<BoardsViewModel>?` (not wrapped)
**Tests:** success returns list, null when workspace doesn't exist
**Call:** `Handler.Handle(new GetBoardsInWorkspaceQuery(), ct)`

#### 3g. `GetBoardsInProjectQueryHandlerTests.cs`
**Handler:** `GetBoardsInProjectQueryHandler(INetptuneUnitOfWork)`
**Return type:** `List<BoardViewModel>?`
**Tests:** `GetBoardsInProject_ShouldReturnCorrectly_WhenValidId`
**Call:** `Handler.Handle(new GetBoardsInProjectQuery(1), ct)`

#### 3h. `IsBoardIdentifierUniqueQueryHandlerTests.cs`
**Handler:** `IsBoardIdentifierUniqueQueryHandler(INetptuneUnitOfWork)`
**Tests:** unique/not-unique cases
**Call:** `Handler.Handle(new IsBoardIdentifierUniqueQuery("identifier"), ct)`

---

### 4. `BoardGroupServiceTests.cs` → Split into per-handler test classes

Location: `Netptune.UnitTests/Netptune.Services/BoardGroups/`

#### 4a. `GetBoardGroupQueryHandlerTests.cs`
**Handler:** `GetBoardGroupQueryHandler(INetptuneUnitOfWork)`
**Return type:** `BoardGroup?`
**Tests:** success returns entity
**Call:** `Handler.Handle(new GetBoardGroupQuery(1), ct)`

#### 4b. `UpdateBoardGroupCommandHandlerTests.cs`
**Handler:** `UpdateBoardGroupCommandHandler(INetptuneUnitOfWork, IActivityLogger)`
**Tests:** success, CompleteAsync called, failure when not found
**Call:** `Handler.Handle(new UpdateBoardGroupCommand(request), ct)`

#### 4c. `CreateBoardGroupCommandHandlerTests.cs`
**Handler:** `CreateBoardGroupCommandHandler(INetptuneUnitOfWork, IActivityLogger)`
**Tests:** success, CompleteAsync called, failure when board not found
**Call:** `Handler.Handle(new CreateBoardGroupCommand(request), ct)`

#### 4d. `DeleteBoardGroupCommandHandlerTests.cs`
**Handler:** `DeleteBoardGroupCommandHandler(INetptuneUnitOfWork, IIdentityService, IActivityLogger)`
**Tests:** success, CompleteAsync called/not, failure when not found
**Call:** `Handler.Handle(new DeleteBoardGroupCommand(1), ct)`

---

### 5. `NotificationServiceTests.cs` → Split into per-handler test classes

Location: `Netptune.UnitTests/Netptune.Services/Notifications/`

#### 5a. `GetUserNotificationsQueryHandlerTests.cs`
**Handler:** `GetUserNotificationsQueryHandler(INetptuneUnitOfWork, IIdentityService)`
**Tests:** returns notifications, queries with correct userId/workspaceId
**Call:** `Handler.Handle(new GetUserNotificationsQuery(), ct)`

#### 5b. `GetUnreadCountQueryHandlerTests.cs`
**Handler:** `GetUnreadCountQueryHandler(INetptuneUnitOfWork, IIdentityService)`
**Tests:** returns count
**Call:** `Handler.Handle(new GetUnreadCountQuery(), ct)`

#### 5c. `MarkAsReadCommandHandlerTests.cs`
**Handler:** `MarkAsReadCommandHandler(INetptuneUnitOfWork, IIdentityService)`
**Tests:** success, not found, belongs to different user
**Call:** `Handler.Handle(new MarkAsReadCommand(id), ct)`

#### 5d. `MarkAllAsReadCommandHandlerTests.cs`
**Handler:** `MarkAllAsReadCommandHandler(INetptuneUnitOfWork, IIdentityService)`
**Tests:** success, calls repository
**Call:** `Handler.Handle(new MarkAllAsReadCommand(), ct)`

---

### 6. `ProjectServiceTests.cs` → Split into per-handler test classes

Location: `Netptune.UnitTests/Netptune.Services/Projects/`

#### 6a. `CreateProjectCommandHandlerTests.cs`
**Handler:** `CreateProjectCommandHandler(INetptuneUnitOfWork, IIdentityService, IActivityLogger)`
**Tests:** success, CompleteAsync, failure when workspace not found
**Call:** `Handler.Handle(new CreateProjectCommand(request), ct)`

#### 6b. `DeleteProjectCommandHandlerTests.cs`
**Handler:** `DeleteProjectCommandHandler(INetptuneUnitOfWork, IIdentityService, IActivityLogger)`
**Tests:** success, CompleteAsync, failure, not calling DeletePermanent/CompleteAsync when not found
**Call:** `Handler.Handle(new DeleteProjectCommand(1), ct)`

#### 6c. `GetProjectQueryHandlerTests.cs`
**Handler:** check handler signature (likely `GetProjectQueryHandler(INetptuneUnitOfWork, IIdentityService)`)
**Tests:** success, null when not found, null when workspace not found
**Call:** `Handler.Handle(new GetProjectQuery("key"), ct)`

#### 6d. `GetProjectsQueryHandlerTests.cs`
**Handler:** check handler signature
**Tests:** returns list
**Call:** `Handler.Handle(new GetProjectsQuery(), ct)`

#### 6e. `UpdateProjectCommandHandlerTests.cs`
**Handler:** `UpdateProjectCommandHandler(INetptuneUnitOfWork, IIdentityService, IActivityLogger)`
**Tests:** success, CompleteAsync, failure when not found
**Call:** `Handler.Handle(new UpdateProjectCommand(request), ct)`

---

### 7. `TaskServiceTests.cs` → Split into per-handler test classes

Location: `Netptune.UnitTests/Netptune.Services/Tasks/`

#### 7a. `CreateTaskCommandHandlerTests.cs`
**Handler:** `CreateTaskCommandHandler(INetptuneUnitOfWork, IIdentityService, IActivityLogger, ILogger<CreateTaskCommandHandler>)`
**Tests:** success + 5 failure cases + activity log
**Call:** `Handler.Handle(new CreateTaskCommand(request), ct)`

#### 7b. `DeleteTaskCommandHandlerTests.cs`
**Handler:** `DeleteTaskCommandHandler(INetptuneUnitOfWork, IActivityLogger)`
**Note:** No `IIdentityService` or `ILogger` needed vs old service
**Tests:** success, DeletePermanent called, CompleteAsync called, failure cases + activity
**Call:** `Handler.Handle(new DeleteTaskCommand(id), ct)`

#### 7c. `DeleteTasksCommandHandlerTests.cs`
**Handler:** `DeleteTasksCommandHandler(INetptuneUnitOfWork, IActivityLogger)`
**Tests:** success, DeletePermanent, CompleteAsync, activity (LogMany)
**Call:** `Handler.Handle(new DeleteTasksCommand(ids), ct)`

#### 7d. `GetTaskQueryHandlerTests.cs`
**Handler:** check handler signature
**Tests:** returns view model
**Call:** `Handler.Handle(new GetTaskQuery(1), ct)`

#### 7e. `GetTaskDetailQueryHandlerTests.cs`
**Handler:** check handler signature
**Tests:** returns detail view model
**Call:** `Handler.Handle(new GetTaskDetailQuery(systemId), ct)`

#### 7f. `GetTasksQueryHandlerTests.cs`
**Handler:** check handler signature
**Tests:** returns list
**Call:** `Handler.Handle(new GetTasksQuery(), ct)`

#### 7g. `UpdateTaskCommandHandlerTests.cs`
**Handler:** `UpdateTaskCommandHandler(INetptuneUnitOfWork, IActivityLogger, ILogger<UpdateTaskCommandHandler>)`
**Tests:** success, failure when not found, Transaction called
**Call:** `Handler.Handle(new UpdateTaskCommand(request), ct)`

#### 7h. `MoveTaskInBoardGroupCommandHandlerTests.cs`
**Handler:** `MoveTaskInBoardGroupCommandHandler(INetptuneUnitOfWork, IActivityLogger)`
**Tests:** transfer (NewGroupId ≠ OldGroupId) success + CompleteAsync + activity;
same-group reorder success
**Call:** `Handler.Handle(new MoveTaskInBoardGroupCommand(request), ct)`

#### 7i. `MoveTasksToGroupCommandHandlerTests.cs`
**Handler:** `MoveTasksToGroupCommandHandler(INetptuneUnitOfWork, IActivityLogger)`
**Tests:** success, CompleteAsync, activity (LogWithMany)
**Call:** `Handler.Handle(new MoveTasksToGroupCommand(request), ct)`

#### 7j. `ReassignTasksCommandHandlerTests.cs`
**Handler:** `ReassignTasksCommandHandler(INetptuneUnitOfWork, IActivityLogger)`
**Tests:** success, CompleteAsync, activity (LogWithMany)
**Call:** `Handler.Handle(new ReassignTasksCommand(request), ct)`

---

### 8. `UserServiceTests.cs` → Split into per-handler test classes

Location: `Netptune.UnitTests/Netptune.Services/Users/`

#### 8a. `GetUserQueryHandlerTests.cs`
**Handler:** `GetUserQueryHandler(INetptuneUnitOfWork, IIdentityService, IWorkspacePermissionCache)`
**Return type:** `UserViewModel?`
**Tests:** success, null when not found
**Call:** `Handler.Handle(new GetUserQuery("userId"), ct)`

#### 8b. `GetUserByEmailQueryHandlerTests.cs`
**Handler:** check constructor (likely same deps as GetUser)
**Return type:** `UserViewModel?`
**Tests:** success, null when not found
**Call:** `Handler.Handle(new GetUserByEmailQuery("email"), ct)`

#### 8c. `GetAllUsersQueryHandlerTests.cs`
**Handler:** check constructor
**Tests:** returns list
**Call:** `Handler.Handle(new GetAllUsersQuery(), ct)`

#### 8d. `GetWorkspaceUsersQueryHandlerTests.cs`
**Handler:** `GetWorkspaceUsersQueryHandler(INetptuneUnitOfWork, IIdentityService)`
**Return type:** `List<WorkspaceUserViewModel>`
**Tests:** returns list, empty when workspace not found
**Call:** `Handler.Handle(new GetWorkspaceUsersQuery(), ct)`

#### 8e. `InviteUsersToWorkspaceCommandHandlerTests.cs`
**Handler:** `InviteUsersToWorkspaceCommandHandler(INetptuneUnitOfWork, IIdentityService, IEmailService, IHostingService, IInviteCache)`
**Tests:** success, workspace not found, empty input, sends emails, filters existing users, CompleteAsync
**Call:** `Handler.Handle(new InviteUsersToWorkspaceCommand(emails), ct)`

#### 8f. `RemoveUsersFromWorkspaceCommandHandlerTests.cs`
**Handler:** `RemoveUsersFromWorkspaceCommandHandler(INetptuneUnitOfWork, IIdentityService, IWorkspaceUserCache, IActivityLogger)`
**Tests:** success, removes from cache, workspace not found, empty input, owner removal fails, CompleteAsync
**Call:** `Handler.Handle(new RemoveUsersFromWorkspaceCommand(emails), ct)`

#### 8g. `UpdateUserCommandHandlerTests.cs`
**Handler:** `UpdateUserCommandHandler(INetptuneUnitOfWork)`
**Note:** Only `INetptuneUnitOfWork` needed
**Tests:** success, CompleteAsync, failure when not found
**Call:** `Handler.Handle(new UpdateUserCommand(request), ct)`

#### 8h. `ToggleUserPermissionCommandHandlerTests.cs`
**Handler:** `ToggleUserPermissionCommandHandler(INetptuneUnitOfWork, IIdentityService, IWorkspacePermissionCache, IActivityLogger)`
**Tests:** add permission, remove permission, workspace not found, user not in workspace,
SetUserPermissions called, CompleteAsync called, cache cleared
**Call:** `Handler.Handle(new ToggleUserPermissionCommand(request), ct)`

---

### 9. `WorkspaceServiceTests.cs` → Split into per-handler test classes

Location: `Netptune.UnitTests/Netptune.Services/Workspaces/`

#### 9a. `CreateWorkspaceCommandHandlerTests.cs`
**Handler:** `CreateWorkspaceCommandHandler(INetptuneUnitOfWork, IIdentityService, IActivityLogger)`
**Note:** Old `WorkspaceService.Create` tests used `IWorkspaceUserCache` — handler doesn't need it
**Tests:** success (Name, Description, Slug), CompleteAsync
**Call:** `Handler.Handle(new CreateWorkspaceCommand(request), ct)`

#### 9b. `DeleteWorkspaceCommandHandlerTests.cs`
**Handler:** `DeleteWorkspaceCommandHandler(INetptuneUnitOfWork, IIdentityService, IWorkspaceUserCache, IActivityLogger)`
**Tests:** success, CompleteAsync, failure when not found, not calling DeletePermanent/Complete
**Call:** `Handler.Handle(new DeleteWorkspaceCommand("workspace"), ct)`

#### 9c. `DeleteWorkspacePermanentCommandHandlerTests.cs`
**Handler:** `DeleteWorkspacePermanentCommandHandler(INetptuneUnitOfWork, IIdentityService, IWorkspaceUserCache)`
**Tests:** success, failure when not found, not calling DeletePermanent/Complete when not found
**Call:** `Handler.Handle(new DeleteWorkspacePermanentCommand("workspace"), ct)`

#### 9d. `GetWorkspaceQueryHandlerTests.cs`
**Handler:** `GetWorkspaceQueryHandler(INetptuneUnitOfWork)`
**Return type:** `Workspace?`
**Tests:** returns entity by slug
**Call:** `Handler.Handle(new GetWorkspaceQuery("slug"), ct)`

#### 9e. `GetUserWorkspacesQueryHandlerTests.cs`
**Handler:** `GetUserWorkspacesQueryHandler(INetptuneUnitOfWork, IIdentityService)`
**Return type:** `List<Workspace>`
**Tests:** returns list for current user
**Call:** `Handler.Handle(new GetUserWorkspacesQuery(), ct)`

#### 9f. `GetAllWorkspacesQueryHandlerTests.cs`
**Handler:** `GetAllWorkspacesQueryHandler(INetptuneUnitOfWork)`
**Return type:** `List<Workspace>`
**Tests:** returns all
**Call:** `Handler.Handle(new GetAllWorkspacesQuery(), ct)`

#### 9g. `UpdateWorkspaceCommandHandlerTests.cs`
**Handler:** `UpdateWorkspaceCommandHandler(INetptuneUnitOfWork, IIdentityService, IActivityLogger)`
**Return type:** `ClientResponse<Workspace>` (not `WorkspaceViewModel`)
**Note:** Old service returned `WorkspaceViewModel`; handler returns `Workspace` entity.
Adjust assertions accordingly.
**Tests:** success, CompleteAsync, failure when not found
**Call:** `Handler.Handle(new UpdateWorkspaceCommand(request), ct)`

#### 9h. `IsWorkspaceSlugUniqueQueryHandlerTests.cs`
**Handler:** `IsWorkspaceSlugUniqueQueryHandler(INetptuneUnitOfWork)`
**Tests:** unique returns `IsUnique = true`, not-unique returns `IsUnique = false`
**Call:** `Handler.Handle(new IsWorkspaceSlugUniqueQuery("slug"), ct)`

---

### 10. `TagServiceTests.cs` → Split into per-handler test classes

Location: `Netptune.UnitTests/Netptune.Services/Tags/`

#### 10a. `CreateTagCommandHandlerTests.cs`
**Handler:** `CreateTagCommandHandler(INetptuneUnitOfWork, IIdentityService, IActivityLogger)`
**Tests:** success, CompleteAsync, failure when workspace not found, failure when tag exists
**Call:** `Handler.Handle(new CreateTagCommand(request), ct)`

#### 10b. `AddTagToTaskCommandHandlerTests.cs`
**Handler:** `AddTagToTaskCommandHandler(INetptuneUnitOfWork, IIdentityService, IActivityLogger)`
**Tests:** success, failure when workspace not found, failure when task not found
**Call:** `Handler.Handle(new AddTagToTaskCommand(request), ct)`

#### 10c. `GetTagsForTaskQueryHandlerTests.cs`
**Handler:** check constructor in `GetTagsForTask/GetTagsForTaskQueryHandler.cs`
**Tests:** returns tags, null when task not found
**Call:** `Handler.Handle(new GetTagsForTaskQuery("task-id"), ct)`

#### 10d. `GetTagsForWorkspaceQueryHandlerTests.cs`
**Handler:** check constructor
**Tests:** returns tags, null when workspace not found
**Call:** `Handler.Handle(new GetTagsForWorkspaceQuery(), ct)`

#### 10e. `DeleteTagsCommandHandlerTests.cs`
**Handler:** `DeleteTagsCommandHandler(INetptuneUnitOfWork, IIdentityService, IActivityLogger)`
**Tests:** success, CompleteAsync, failure when workspace not found
**Call:** `Handler.Handle(new DeleteTagsCommand(request), ct)`

#### 10f. `DeleteTagFromTaskCommandHandlerTests.cs`
**Handler:** check constructor
**Tests:** success, CompleteAsync, failure when workspace not found
**Call:** `Handler.Handle(new DeleteTagFromTaskCommand(request), ct)`

#### 10g. `UpdateTagCommandHandlerTests.cs`
**Handler:** check constructor
**Tests:** success, CompleteAsync, workspace not found, tag not found, whitespace trimmed
**Call:** `Handler.Handle(new UpdateTagCommand(request), ct)`

---

### 11. `CommentServiceTests.cs` → Split into per-handler test classes

Location: `Netptune.UnitTests/Netptune.Services/Comments/`

#### 11a. `AddCommentToTaskCommandHandlerTests.cs`
**Handler:** `AddCommentToTaskCommandHandler(INetptuneUnitOfWork, IIdentityService, IActivityLogger)`
**Tests:** success, task not found, workspace not found
**Call:** `Handler.Handle(new AddCommentToTaskCommand(request), ct)`

#### 11b. `GetCommentsForTaskQueryHandlerTests.cs`
**Handler:** check constructor
**Tests:** returns comments, null when task not found
**Call:** `Handler.Handle(new GetCommentsForTaskQuery("task-id"), ct)`

#### 11c. `DeleteCommentCommandHandlerTests.cs`
**Handler:** check constructor
**Tests:** success, CompleteAsync, failure when not found, not calling CompleteAsync when not found
**Call:** `Handler.Handle(new DeleteCommentCommand(1), ct)`

---

## Cleanup After Migration

Once all new handler test files are written and the build passes:

1. Delete the old flat service test files:
   - `ActivityServiceTests.cs`
   - `BoardGroupServiceTests.cs`
   - `BoardServiceTests.cs`
   - `CommentServiceTests.cs`
   - `NotificationServiceTests.cs`
   - `ProjectServiceTests.cs`
   - `TagServiceTests.cs`
   - `TaskServiceTests.cs`
   - `UserServiceTests.cs`
   - `WorkspaceServiceTests.cs`

2. Run the full test suite to verify all new tests pass.

## Notes on `CreateWorkspaceForNewUserCommandHandler`

`WorkspaceServiceTests.cs` contains `CreateNewUserWorkspace_ShouldReturnCorrectly_WhenInputValid`
which tests `Service.CreateNewUserWorkspace(request, user)`. This method maps to
`CreateWorkspaceForNewUserCommandHandler(INetptuneUnitOfWork)` (no Identity/Activity needed).
The handler just delegates to `WorkspaceFactory.CreateAsync`. Add this as a test in
`CreateWorkspaceForNewUserCommandHandlerTests.cs` alongside the `CreateWorkspaceCommandHandler` tests.
