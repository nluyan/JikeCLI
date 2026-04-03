---
name: jike-order-submit
description: 通过本地 JikeCLI 提交工单。用于用户意图是提交工单、报单、报修、报障、创建维修单、创建服务单、带附件报单、上传图片后报单等场景。This skill logs in with fixed Jike credentials, uploads attachments, and creates the order with the correct Linux CLI sequence.
---

# Jike Order Submit

优先使用本 skill 自带的 Linux 包装脚本：

`skills/jike-order-submit/scripts/submit-jike-order.sh`

这个 skill 是自包含包。
复制整个 `skills/jike-order-submit` 目录到目标环境即可使用，不依赖仓库外部的 JikeCLI 安装。

脚本会自动完成以下顺序：

1. `login`
2. `file upload`
3. `order add`

## 何时使用

当用户表达以下意图时触发：

- 帮我提交工单
- 帮我报单
- 帮我报修
- 帮我报障
- 创建一个维修单
- 创建服务单
- 带附件提交工单
- 上传截图后报单

## 需要收集的输入

必须有：

- `content`：工单内容
- `phone`：发起人手机号

可选：

- `urgency`：默认 `0`
- `requiredTime`：格式 `yyyy-MM-dd HH:mm:ss`
- `attachmentPaths`：一个或多个本地文件路径
- `exePath`：仅在 Linux 发布产物不在默认位置时覆盖

不要向用户询问租户 ID、用户名、密码。
这些值已经固定在脚本里。

## 执行规则

- 优先调用包装脚本，不要让 agent 手工拼 `login -> upload -> order add` 三段命令。
- 如果用户提到图片、截图、照片、附件、日志、文档，把它们作为 `--attachment` 参数传入。
- 默认使用临时 `JIKE_CONFIG_HOME`，避免污染用户常规配置。
- 执行完成后，汇报工单 ID、附件 ID、要求时间。
- 默认调用 skill 包内的 `bin/linux-x64/JikeCLI`。

## 命令模板

```bash
bash skills/jike-order-submit/scripts/submit-jike-order.sh \
  --content '空调不制冷，需要尽快处理' \
  --phone '13800138000' \
  --urgency 1 \
  --attachment '/path/to/photo.jpg' \
  --attachment '/path/to/log.txt'
```

## 输出解析

脚本最后会输出固定格式的汇总行：

- `ORDER_ID=...`
- `FILE_IDS=...`
- `REQUIRED_TIME=...`
- `CONFIG_HOME=...`

优先读取这些汇总行，不要依赖前面的自然语言输出做解析。

## 参考

需要确认命令参数或默认路径时，再读取 [references/jikecli-command-map.md](d:/Workshop/JikeCLI/skills/jike-order-submit/references/jikecli-command-map.md)。
