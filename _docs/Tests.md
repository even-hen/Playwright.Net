# Application Functionality & Test Coverage Matrix

This document maps the core features of the Nest application to their corresponding test coverage in the backend API test suite (`Api.Tests`) and the frontend end-to-end UI test suite (`E2E.Tests`).

## Coverage Summary Table

| Module / Feature | Functionality Description | API Test Coverage (`tests/Api.Tests/`) | E2E UI Test Coverage (`tests/E2E.Tests/`) |
| :--- | :--- | :--- | :--- |
| **Authentication & Onboarding** | User registration and account creation | `TC_AUTH_001_SuccessfulRegistration` | `TC_E2E_001_FullRegistration_RedirectsToGroupSetup` |
| | User authentication (login) | `TC_AUTH_010_SuccessfulLogin` | `TC_E2E_002_Login_RedirectsToTabBar_WhenUserHasGroup` |
| | Login error validations | `TC_AUTH_011_LoginFails_WrongPassword`<br>`TC_AUTH_012_LoginFails_NonExistentEmail` | `TC_E2E_004_LoginWithInvalidCredentialsShowsError` |
| | Navigation between auth views | *N/A (Frontend navigation concern)* | `TC_E2E_005_NavigateBetweenLoginAndRegister` |
| | Session termination (logout) | `TC_AUTH_020_SuccessfulSignOut` | `TC_E2E_034_LeaveGroup_And_SignOut` |
| | Password recovery | `TC_AUTH_030_ResetPasswordRequest` | *Not covered on UI due to rate limits* |
| **Group Setup & Management** | Group creation | `TC_GROUP_001_CreateGroupWithValidName` | `TC_E2E_007_CreateGroup_RedirectsToAssignments` |
| | Group settings inspection | `TC_GROUP_010_FetchGroupSettings` | *Implicitly loaded in UI tabs* |
| | Automatic task distribution configuration | `TC_GROUP_020_ToggleAutoDistribution` | *Toggle behavior in Admin panel* |
| | Leaving a group | *N/A (Covered via Cascade delete APIs)* | `TC_E2E_034_LeaveGroup_And_SignOut` |
| **Invite Links** | Code generation for invites | `TC_INVITE_001_GenerateValidInviteCode` | `TC_E2E_033_GenerateInviteCode` |
| | Joining a group using code | `TC_INVITE_010_JoinGroupWithValidCode` | `TC_E2E_009_JoinGroupWithInvalidCodeShowsError` (Invalid error check) |
| **User Profile Management** | Fetching profiles | `TC_USER_001_FetchUserProfileById`<br>`TC_USER_003_FetchAllUsersInGroup` | `TC_E2E_022_ViewMembers_And_NavigateWeeks` (Members tab load) |
| | Profile details update (e.g. name) | `TC_USER_010_UpdateUserName_AdultUpdatesSelf` | *Implicitly verified via UI forms* |
| | Adjusting resources/time allowance | `TC_USER_012_UpdateUserResource_AdultUpdatesSelf` | *Input validated in settings* |
| | Modifying user role types | `TC_USER_013_UpdateUserType_AdultUpdatesTeenToChild` | *UI lists member types* |
| | User Notification Preferences | `TC_USER_014_UpdateNotificationTime` | `TC_E2E_032_ChangeNotificationTimeAndTheme` |
| | User timezone settings | `TC_USER_015_UpdateTimezone` | *Automatically synced from browser* |
| **Tasks Management** | Task creation with metadata | `TC_TASK_001_CreateTaskWithAllValidFields` | `TC_E2E_017_CreateNewTask_ViaModal` |
| | Viewing & filtering tasks list | *Implicit via get/list endpoint checks* | `TC_E2E_016_ViewTaskList_And_Search` |
| | Editing task configuration | `TC_TASK_020_UpdateTaskTitle` | *Modify task settings* |
| | Deactivating / archiving tasks | `TC_TASK_022_DeactivateTask` | *Archival options in Task views* |
| | Task removal | `TC_TASK_040_DeleteTaskSuccessfully` | *Delete action in Task view* |
| **Assignments & Workloads** | Assignment creation and query | `TC_ASSIGN_001_CreateAndQueryAssignment` | `TC_E2E_011_ViewAssignments_And_MarkAsDone` (Read workload) |
| | Completing/Marking assignments done | `TC_ASSIGN_010_MarkAssignmentAsDone` | `TC_E2E_011_ViewAssignments_And_MarkAsDone` (Check off UI) |
| | Workload tracking & history | *Implicit in historical queries* | `TC_E2E_022_ViewMembers_And_NavigateWeeks` (Weekly paging) |
| **Notifications** | Reading notifications | `TC_NOTIF_001_MarkNotificationAsRead` | `TC_E2E_031_MarkAllNotificationsAsRead` |
| | Checking notifications feed | *Implicit in list checks* | `TC_E2E_030_ViewNotificationsList_And_UnreadDot` |
| **Role-Based Access Control** | Admin UI layout (Adult) | *Permissions checked by APIs* | `TC_E2E_037_AdultSeesAllAdminUIElements` |
| | Non-admin restrictions (Teen/Child) | *Restriction validations in update APIs* | `TC_E2E_036_TeenSeesRestrictedUIElements` |
| **Responsive Viewports** | Mobile layout and drawer/tab navigation | *N/A (Backend API is viewport independent)* | `TC_E2E_040_MobileViewport_ResponsiveLayout` |
| | Desktop viewport layouts | *N/A (Backend API is viewport independent)* | `TC_E2E_041_DesktopViewport_FullLayout` |
