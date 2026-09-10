# DropKick Loader

Windows / Steam normal launch / Project Zomboid single-player.

这是飞踢模组的专用 Java 桥接和一次性启动配置工具，不是完整模组。Lua、动画和音效需要另行安装 DropKick 模组；本仓库暂不提供工坊订阅链接。

## 中文安装说明

当前发布为候选版：作者已实测 Steam 正常启动、保存重进、当前多模组组合，以及卸载后启动和重装恢复。未验证所有电脑或模组组合。

1. 下载 Releases 中的 `DropKickLoader-1.0.0-rc.2-windows.zip`，完整解压，不要在压缩包内运行。
2. 完全退出游戏和 Steam（包括后台）。
3. 双击 `installer/DropKickSetup.exe`。
4. 点击“安装 / 更新”
5. 重新打开 Steam，正常启动游戏。

### 卸载与更新

图形入口中的“卸载加载器”只卸载 Java 加载部分，不取消工坊订阅，不删除飞踢模组文件、其他模组或存档。卸载后飞踢还会显示，但不可用，空格恢复原版推搡；重新安装加载器后可恢复飞踢功能。

卸载会将本工具的 JAR、启动包装 EXE 及安装记录移入游戏目录里的 `DropKickLoader-backups` 和 `DropKickSteamLaunch-backups`，并恢复安装时记录的原 Steam 启动选项。它不是存档清理工具，不会从存档中删除技能数据。

不建议采用别的方法自行卸载，可能会出现问题。

## English

This is the dedicated loader, not the complete DropKick mod. Install the matching Lua/animation/audio mod separately. A Workshop link is not yet supplied.

1. Download and extract the rc.2 Windows ZIP.
2. Exit Steam/game and back up saves.
3. Double-click `installer/DropKickSetup.exe`. 
4. Click **Install / update** and confirm. 
5. Restart Steam and normally Play.

To uninstall, close Steam/game and click **Uninstall loader**. Original launch options are restored and owned files are moved to recoverable backups.

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
