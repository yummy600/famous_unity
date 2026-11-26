# Famous Great Project

Unity `6000.3.21f1` 多人 3D 原型，使用 Netcode for GameObjects `2.7.0` 和 Unity Transport `2.6.0`。无需账号或云服务，首次导入需下载 Unity 包。

## 双实例运行

1. 用 Unity Hub 打开项目根目录，打开 `Assets/Game/Scenes/Arena.unity`。若场景不存在，使用菜单 `Famous Great Game > Create or Open Arena` 生成。
2. 在 `File > Build Profiles` 选择本机桌面平台，确认 `Arena` 在场景列表中，执行 `Build And Run`，产物可放到 `Builds/`。
3. 在独立应用点 `Host`。回到编辑器进入 Play Mode，在游戏视图使用 `127.0.0.1`、端口 `7777`，点 `Join`。局域网测试时改填主机的局域网 IPv4，并确保防火墙允许该 UDP 端口。
4. 用 `WASD` 或方向键移动。当前实例的角色为青色，其他角色为橙色。客户端退出时对应角色消失；主机结束会话时客户端断开。

当前为最多 8 人（含主机）的直连原型。主机计算移动，客户端只发送输入。尚未接入公网中继、账号、匹配或独立服务器。

## macOS 联机验收

本机已构建 `Builds/ArenaSmoke.app`，可直接打开并跳过手动构建。自动验收通过两个独立进程验证了双方各自看到本地和远端角色、客户端移动同步、客户端退出后主机清理角色。日志位于 `Logs/arena-smoke-host.log` 和 `Logs/arena-smoke-client.log`。

复跑时，可用 Unity 批处理入口 `FamousGreatGame.Editor.ArenaSetup.BuildMacSmokePlayer` 构建开发版，然后分别给两个可执行进程传入 `-batchmode -nographics -arena-smoke-host` 和 `-batchmode -nographics -arena-smoke-client`（先启动主机，使用本机 UDP 7777）。两端会输出 `ARENA_SMOKE PASS` 并以状态码 0 结束。