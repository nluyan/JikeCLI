# JikeCLI Command Map

skill 内默认可执行文件路径：

`skills/jike-order-submit/bin/linux-x64/JikeCLI`

命令：

- `login --username <string> --tenant <string> --password <string>`
- `file upload --path <string>`
- `order add --phone <string> --urgency <0|1> --content <string> [--requiredTime "yyyy-MM-dd HH:mm:ss"] [--files "id1,id2"]`

要点：

- `order add --files` 需要附件 ID，不是本地路径。
- `login` 成功后才可以 `upload` 和 `order add`。
- `JIKE_CONFIG_HOME` 可覆盖配置目录，适合自动化。
- skill 的包装脚本已经写死租户、用户名、密码，不需要 agent 再收集这些值。
- 整个 skill 目录可以直接复制到 Linux 环境使用，前提是目标环境满足该二进制的运行依赖。

常见触发表达：

- 提交工单
- 报单
- 报修
- 报障
- 创建维修单
- 带附件报单
