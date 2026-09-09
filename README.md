# DropKick Loader

Windows / Steam normal launch / Project Zomboid single-player.

这是飞踢模组的专用 Java 桥接和一次性启动配置工具，不是完整模组。Lua、动画和音效需要另行安装 DropKick 模组；本仓库暂不提供工坊订阅链接。

## 中文安装说明

当前发布为候选版：作者已实测 Steam 正常启动、保存重进、当前多模组组合，以及卸载后启动和重装恢复。未验证所有电脑或模组组合。

1. 下载 Releases 中的 `DropKickLoader-1.0.0-rc.2-windows.zip`，完整解压，不要在压缩包内运行。
2. 完全退出游戏和 Steam（包括托盘）。备份存档。
3. 双击 `installer/DropKickSetup.exe`。程序自动查找 Steam 注册路径、常规安装位置及其他游戏库，唯一候选会自动填写；多个候选需从下拉框选择，也可浏览或手动填写。确认正确的 Steam 账号配置。
4. 点击“安装 / 更新”，核对确认框后执行。界面随 Windows UI 语言切换中文或英文，无需填写命令，也没有 Preview 按钮。

下方命令仅供高级用户排查问题，普通玩家不需要执行：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "解压目录\installer\Setup-Steam.ps1" -Mode Install -GameDirectory "游戏目录" -SteamLocalConfig "Steam安装目录\userdata\账号数字目录\config\localconfig.vdf"
```

`ExecutionPolicy Bypass` 只用于这次 PowerShell 进程，不修改系统持久策略。仅运行你信任的安装包；不要关闭安全软件。如果权限不足，先确认报错目标与路径，不要盲目提权。

5. 重新打开 Steam，选择普通启动，启用匹配版本的 DropKick 模组。以后不需要再运行安装脚本或手填启动参数。不要双击 `DropKickSteamLaunch.exe` 启动，也不要再添加旧的 `-javaagent` 参数。

### 卸载与更新

图形入口中的“卸载加载器”只卸载 Java 加载部分，不取消工坊订阅，不删除飞踢模组文件、其他模组或存档，也不会移除 ReflectionEnabler。卸载后飞踢暂时不可用，空格保留原版推搡；重新安装兼容加载器后可恢复飞踢。

卸载会将本工具的 JAR、启动包装 EXE 及安装记录移入游戏目录里的 `DropKickLoader-backups` 和 `DropKickSteamLaunch-backups`，并恢复安装时记录的原 Steam 启动选项。它不是存档清理工具，不会从存档中删除技能数据。若另行停用飞踢模组，建议先备份存档。失败时可能留下部分文件或备份，应保留错误信息核对，不要自行删除整个备份目录或覆盖完整 Steam 配置。

完全退出 Steam 和游戏，在图形界面点击“卸载加载器”，阅读影响说明后确认。然后可停用模组；从有自定义技能的存档移除模组前仍须备份。

安装后若手动改过 Steam 启动选项，脚本会拒绝覆盖，请先人工核对，不要直接覆盖整个 Steam 配置备份。备份可能包含账号私有配置，不要公开上传。

更新加载器时使用新版包点击“安装 / 更新”；图形入口的安装、卸载和重装已由作者实测，任意跨版本升级仍不保证。工坊更新 Lua 不会自动更新游戏目录里的 JAR。游戏升级导致指纹不匹配时等待适配，勿绕过校验。

重要：配套 DropKick Lua 必须包含缺失桥接检测修复。旧版 Lua 在卸载加载器后可能于主菜单及按键时反复报错；仅更新安装器不能修复旧 Lua。2026-09-10 修复版会在调用前检查三个桥接函数，并优先过滤非飞踢按键。

## English

This is the dedicated loader, not the complete DropKick mod. Install the matching Lua/animation/audio mod separately. A Workshop link is not yet supplied.

Download and extract the rc.2 Windows ZIP. Exit Steam/game and back up saves. Double-click `installer/DropKickSetup.exe`. Standard Steam locations and library folders are detected; unique candidates are filled automatically. Choose the correct entry when multiple candidates exist, or browse manually. Click **Install / update** and confirm. The UI follows the Windows language (Chinese/English) and has no Preview button. The command below is an advanced alternative, not required for normal installation.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "EXTRACTED\installer\Setup-Steam.ps1" -Mode Install -GameDirectory "GAME_DIRECTORY" -SteamLocalConfig "STEAM_DIRECTORY\userdata\YOUR_ACCOUNT_ID\config\localconfig.vdf"
```

Confirm the correct Steam profile if several accounts exist. Restart Steam and use normal Play, not Alternate Launch. No per-launch script, manual agent argument, or separate JDK is needed. Windows .NET Framework is required by the wrapper; Java comes from the game. ExecutionPolicy Bypass applies only to this process. Do not disable security software.

To uninstall, close Steam/game and click **Uninstall loader**. Original launch options are restored and owned files are moved to recoverable backups. Modified launch options or unknown installed files cause refusal instead of overwrite. Do not publish Steam configuration backups. Use **Install / update** with a newer compatible loader to update; Workshop subscriptions do not update the installed JAR automatically.

Use the matching DropKick Lua with the 2026-09-10 missing-bridge guard fix. Old Lua can repeatedly log exceptions after loader removal; updating this installer alone cannot fix old Lua. The author has verified GUI uninstall/reinstall and gameplay with the corrected Lua.

## Compatibility and implementation

### What uninstall changes

The GUI's **Uninstall loader** action restores the recorded pre-install Steam launch options and moves the owned JAR, wrapper EXE and receipts into `DropKickLoader-backups` / `DropKickSteamLaunch-backups` in the game directory. It does not unsubscribe from Workshop, delete saves or mod files, or remove ReflectionEnabler. DropKick becomes unavailable; native shove remains. Reinstall a compatible loader to restore DropKick. Uninstall does not clean skill data from saves; back up saves before separately disabling the mod. On failure, partial files/backups may remain: retain the error and review rather than deleting backup folders or restoring an entire Steam profile indiscriminately.

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
