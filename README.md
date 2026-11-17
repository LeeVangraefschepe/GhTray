# GhTray

A tray app for Windows and Linux that watches GitHub for pull requests and issues that need your attention.

- Lists open PRs assigned to you, PRs awaiting your review, and issues assigned to you
- Shows CI check results per PR and lists failing checks on PRs assigned to you
- Desktop notification for new assignments, updates and newly failing checks (your own pushes and comments are ignored)
- Tray icon badge: red for unseen activity, yellow when fetching from GitHub fails
- Optional start at login

## Requirements

All platforms:

- [GitHub CLI](https://cli.github.com) signed in with `gh auth login`. GhTray shells out to `gh`, so it uses whichever account `gh` is authenticated as and stores no credentials itself.
- [.NET 10 SDK](https://dotnet.microsoft.com/download) to build from source

Windows 10/11: nothing else.

Ubuntu (22.04 or newer, X11 or Wayland through XWayland):

- A tray host. Ubuntu Desktop ships the AppIndicator extension enabled by default. On other GNOME setups install and enable *AppIndicator and KStatusNotifierItem Support*. KDE, XFCE and Cinnamon work out of the box.
- `notify-send` for notifications (`sudo apt install libnotify-bin`, preinstalled on Ubuntu Desktop). Clicking a notification to open it needs libnotify 0.8 or newer (Ubuntu 22.10+); older versions still show it.
- `xdg-open` to open links (preinstalled on desktop installs).

## Build and run

```sh
dotnet run --project GhTray
```

## Publish

```sh
# Windows
dotnet publish GhTray -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish/win-x64

# Linux (self-contained, no .NET install needed on the target)
dotnet publish GhTray -c Release -r linux-x64 --self-contained -o publish/linux-x64
```

Run `publish/linux-x64/GhTray` on the target machine. State is kept in `~/.local/share/GhTray` on Linux and `%LOCALAPPDATA%\GhTray` on Windows.

## Platform notes

| Concern          | Windows                                         | Linux                                              |
| ---------------- | ----------------------------------------------- | -------------------------------------------------- |
| Tray icon        | Notification area                               | StatusNotifierItem (AppIndicator)                  |
| Notifications    | Toast through a hidden PowerShell process       | `notify-send`                                      |
| Start at login   | `HKCU\...\Run` registry value                   | `~/.config/autostart/ghtray.desktop`               |
| Icon colour      | Follows the taskbar theme                       | White on GNOME/Ubuntu, otherwise follows the theme |
| Notification click | Opens the item (list notifications do nothing) | Opens the item, or the list for summaries          |

## Project layout

| Folder        | Responsibility                                                             |
| ------------- | -------------------------------------------------------------------------- |
| `Models/`     | Immutable records: work items, check summaries, detected changes           |
| `GitHub/`     | `gh` process wrapper, JSON parsers, activity analysis, `GitHubClient`      |
| `Storage/`    | Persisted "already seen" state                                             |
| `Tray/`       | Tray icon, notification composition, polling orchestration                 |
| `Platform/`   | Per-OS autostart, notifications and panel theme behind small interfaces    |
| `Icons/`      | Tray icon rendering with Avalonia drawing primitives                       |
| `UI/`         | The list window (Avalonia, built in code): tabs, rows, header, status bar  |
| `Formatting/` | Text truncation and relative timestamps                                    |
