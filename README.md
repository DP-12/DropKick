# DropKick Loader

Windows / Steam normal launch / Project Zomboid single-player.

这是飞踢模组的专用 Java 桥接和一次性启动配置工具，不是完整模组。Lua、动画和音效需要另行安装 DropKick 模组；本仓库暂不提供工坊订阅链接。

## 中文安装说明

当前发布为候选版：作者已实测 Steam 正常启动、保存重进、当前多模组组合，以及卸载后启动和重装恢复。未验证所有电脑或模组组合。

1. 下载 Releases 中的 `DropKickLoader-1.0.0-rc.1-windows.zip`，完整解压，不要在压缩包内运行。
2. 完全退出游戏和 Steam（包括托盘）。备份存档。
3. 找到游戏目录（里面有 `ProjectZomboid64.exe` 和 `projectzomboid.jar`），以及你的 Steam 账号配置 `Steam安装目录\userdata\账号数字目录\config\localconfig.vdf`。多个账号时不要猜测；确认你玩游戏所用账号的目录。
4. 打开 PowerShell，按下面模板填入三个真实路径。先加 `-WhatIf` 预览，确认后去掉它执行。预览不是完整兼容性验证，实际安装还会检查类指纹。

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "解压目录\installer\Setup-Steam.ps1" -Mode Install -GameDirectory "游戏目录" -SteamLocalConfig "Steam安装目录\userdata\账号数字目录\config\localconfig.vdf" -WhatIf
```

`ExecutionPolicy Bypass` 只用于这次 PowerShell 进程，不修改系统持久策略。仅运行你信任的安装包；不要关闭安全软件。如果权限不足，先确认报错目标与路径，不要盲目提权。

5. 重新打开 Steam，选择普通启动，启用匹配版本的 DropKick 模组。以后不需要再运行安装脚本或手填启动参数。不要双击 `DropKickSteamLaunch.exe` 启动，也不要再添加旧的 `-javaagent` 参数。

### 卸载与更新

完全退出 Steam 和游戏，使用同一命令，将 `-Mode Install` 改为 `-Mode Uninstall`，不加 `-WhatIf`。它恢复记录的原启动选项，并把自己的文件移入游戏目录内的备份文件夹，不删除存档或 ReflectionEnabler。然后可停用模组；从有自定义技能的存档移除模组前仍须备份。

安装后若手动改过 Steam 启动选项，脚本会拒绝覆盖，请先人工核对，不要直接覆盖整个 Steam 配置备份。备份可能包含账号私有配置，不要公开上传。

更新加载器时使用新版包再次执行 Install；同版本重装已测试，不同版本升级仍需验证。工坊更新 Lua 不会自动更新游戏目录里的 JAR。游戏升级导致指纹不匹配时等待适配，勿绕过校验。

## English

This is the dedicated loader, not the complete DropKick mod. Install the matching Lua/animation/audio mod separately. A Workshop link is not yet supplied.

Download and extract the release ZIP. Exit Steam and the game completely. Back up saves. Run the command below in PowerShell after replacing all three paths. Add `-WhatIf` for a no-write preview; remove it to install. The preview does not perform every compatibility check.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "EXTRACTED\installer\Setup-Steam.ps1" -Mode Install -GameDirectory "GAME_DIRECTORY" -SteamLocalConfig "STEAM_DIRECTORY\userdata\YOUR_ACCOUNT_ID\config\localconfig.vdf"
```

Confirm the correct Steam profile if several accounts exist. Restart Steam and use normal Play, not Alternate Launch. No per-launch script, manual agent argument, or separate JDK is needed. Windows .NET Framework is required by the wrapper; Java comes from the game. ExecutionPolicy Bypass applies only to this process. Do not disable security software.

To uninstall, close Steam and the game and rerun with `-Mode Uninstall`. Original launch options are restored and owned files are moved to recoverable backups. Modified launch options or unknown installed files cause refusal instead of overwrite. Do not publish Steam configuration backups. Rerun Install with a newer compatible loader to update; Workshop subscriptions do not update the installed JAR automatically.

## Compatibility and implementation

- Java 25 and exact supported game-class fingerprints are required. This is not blanket support for all Build 42 versions.
- The packaged agent accepts vanilla classes and the audited ReflectionEnabler v2 GlobalObject for 42.20.2 (SHA-256 `4915be279b76f53f0e2eaabe529ef026f23e8c0066a21f48f887a3cb99ad9f8f`). ReflectionEnabler is optional and is not bundled, removed, or installed by this tool. Other revisions may be refused.
- No ZombieBuddy dependency. Multiplayer is unsupported.
- The agent hooks Lua initialization in memory and exposes dedicated impulse and blood-particle functions. No original game class is patched on disk.
- The wrapper supplies the agent argument and Java DLL search directories only to its child game process. It does not modify system PATH.
- Setup modifies this game's launch option in the selected Steam profile, installs its own EXE/JAR and receipts, and creates backups. Saves and other mods are not changed.
- Unknown JVM wrappers/options and inherited Java option environment variables are refused. This is intentionally conservative, not a universal loader compatibility layer.
- Software is unsigned. This candidate is not a guarantee against crashes or future game changes. Core gameplay has been tested on the author's environment; a clean third-party installation remains to be tested.

## Build from source

Developer requirements: JDK 25 and your own legally installed game JAR. No game binaries are redistributed.

```powershell
./build.ps1 -JdkHome "YOUR_JDK_25" -GameJar "YOUR_GAME/projectzomboid.jar"
```

This generates `build/DropKickLoader.jar`. Generating new fingerprints is not proof of compatibility: audit changed APIs and test before distributing. Compile `installer/SteamLaunch.cs` using the Windows .NET Framework C# compiler with `/target:winexe /platform:x64 /reference:System.Windows.Forms.dll` to produce the wrapper.

No open-source license has been selected by the author yet; public source visibility is not a grant of an additional license.
