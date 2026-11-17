# GhTray

A Windows tray app that watches GitHub for pull requests and issues that need your attention.

- Lists open PRs assigned to you, PRs awaiting your review, and issues assigned to you
- Shows CI check results per PR and lists failing checks on PRs assigned to you
- Balloon notification for new assignments, updates and newly failing checks (your own pushes and comments are ignored)
- Tray icon badge: red for unseen activity, yellow when fetching from GitHub fails
- Optional start with Windows

## Requirements

- Windows 10/11
- [.NET 10 SDK](https://dotnet.microsoft.com/download) to build, .NET 10 Desktop Runtime to run
- [GitHub CLI](https://cli.github.com) signed in with `gh auth login`

GhTray shells out to `gh`, so it uses whichever account `gh` is authenticated as and stores no credentials itself.

## Build and run

```powershell
dotnet run --project GhTray
```

## Publish

```powershell
dotnet publish GhTray -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish
```

## Project layout

| Folder        | Responsibility                                                       |
| ------------- | -------------------------------------------------------------------- |
| `Models/`     | Immutable records: work items, check summaries, detected changes     |
| `GitHub/`     | `gh` process wrapper, JSON parsers, activity analysis, `GitHubClient` |
| `Storage/`    | Persisted "already seen" state under `%LOCALAPPDATA%\GhTray`         |
| `Tray/`       | Tray icon, balloon notifications, autostart, polling orchestration   |
| `Icons/`      | Tray icon rendering and the SVG path parser behind it                |
| `UI/`         | The list window: tabs, owner-drawn rows, status strip                |
| `Formatting/` | Text truncation and relative timestamps                              |
