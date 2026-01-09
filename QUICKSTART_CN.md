# 快速开始指南

## 📋 前提条件

1. **RimWorld** 1.5或更高版本
2. **Harmony** 模组
3. **RimTalk** 模组（已配置API密钥）

## 🚀 5分钟快速开始

### 步骤1：设置开发环境

```powershell
# 克隆或下载此仓库
cd RimTalk-Quests

# 运行环境设置脚本（仅需一次）
.\setup-env.ps1
```

这会自动检测你的RimWorld安装路径并设置`RIMWORLD_DIR`环境变量。

### 步骤2：构建模组

```powershell
# 使用构建脚本
.\build.ps1

# 或指定配置
.\build.ps1 -GameVersion 1.6 -Configuration Release
```

构建成功后，DLL会自动部署到：
```
<RimWorld安装目录>\Mods\RimTalk-Quests\
```

### 步骤3：在RimWorld中启用

1. 启动RimWorld
2. 进入**模组**菜单
3. 确保加载顺序：
   ```
   ☑ Harmony
   ☑ RimTalk
   ☑ RimTalk - AI Quests  ← 新模组
   ```
4. 重启游戏

### 步骤4：配置（可选）

1. 进入**选项** → **模组设置** → **RimTalk - AI Quests**
2. 调整设置：
   - ✅ **启用AI任务描述**（默认开启）
   - 💾 **缓存生成的描述**（推荐开启）

### 步骤5：享受！

开始新游戏或加载存档，当任务生成时，你会看到AI生成的描述！

## 🎯 测试模组

### 快速测试

1. 在游戏中按 `F10` 打开开发模式
2. 按 `~` 打开调试控制台
3. 输入命令测试任务生成：
   ```
   QuestGen_GenerateTest OpportunitySite_ItemStash
   ```

### 查看日志

AI生成过程会在日志中输出：
```
[RimTalk-Quests] Generating AI description for quest: ...
[RimTalk-Quests] Successfully generated AI description for: ...
```

按 `Ctrl+F12` 打开开发日志查看详细信息。

## 🔧 常见问题

### 问题1：构建失败 - 找不到RimWorld

**症状**：
```
错误: RimWorld目录不存在
```

**解决方案**：
```powershell
# 手动设置环境变量
$env:RIMWORLD_DIR = "你的RimWorld路径"

# 例如：
$env:RIMWORLD_DIR = "C:\Program Files (x86)\Steam\steamapps\common\RimWorld"

# 然后重新构建
.\build.ps1
```

### 问题2：RimTalk.dll引用错误

**症状**：
```
警告: 未能解析引用 RimTalk.dll
```

**解决方案**：

方案A - 确保RimTalk已安装：
```
<RimWorld>\Mods\RimTalk\1.6\Assemblies\RimTalk.dll
```

方案B - 使用NuGet包（无需本地RimTalk）：
```powershell
dotnet build /p:UseLocalDlls=false
```

### 问题3：游戏中无效果

**检查清单**：
1. ✅ RimTalk是否已配置API密钥？
   - 选项 → 模组设置 → RimTalk → 输入API密钥
2. ✅ 模组加载顺序正确？
   - Harmony → RimTalk → RimTalk-Quests
3. ✅ AI描述已启用？
   - 选项 → 模组设置 → RimTalk - AI Quests
4. ✅ 查看日志是否有错误？
   - 开发模式 → Ctrl+F12 查看日志

### 问题4：描述生成很慢

**正常情况**：
- 首次生成：2-5秒（取决于AI API响应时间）
- 后续显示：即时（使用缓存）

**优化建议**：
- ✅ 启用"缓存生成的描述"
- 使用更快的AI模型（如Gemini Flash）

## 📚 进阶使用

### 自定义提示词

编辑 `QuestDescriptionGenerator.cs` 中的 `BuildSystemInstruction()` 方法：

```csharp
private static string BuildSystemInstruction()
{
    return @"你的自定义系统提示词...";
}
```

### 添加更多上下文

编辑 `BuildQuestPrompt()` 方法添加更多上下文信息：

```csharp
// 添加殖民地财富
sb.AppendLine($"Colony wealth: {map.wealthWatcher.WealthTotal}");

// 添加当前威胁
if (Find.Storyteller.difficulty.threatScale > 1.0f)
    sb.AppendLine("Colony under high threat");
```

### 禁用任务名称生成

如果只想修改描述，不修改名称，注释掉 `QuestNameGetterPatch`：

```csharp
// [HarmonyPatch(typeof(Quest))]
// [HarmonyPatch("get_name")]
// public static class QuestNameGetterPatch
// {
//     ...
// }
```

## 🎓 学习资源

### RimWorld模组开发
- [RimWorld Wiki - Modding](https://rimworldwiki.com/wiki/Modding)
- [RimWorld Discord - #mod-development](https://discord.gg/rimworld)

### Harmony补丁
- [Harmony文档](https://harmony.pardeike.net/)
- [Harmony教程](https://rimworldwiki.com/wiki/Harmony)

### RimTalk集成
- [RimTalk源码](https://github.com/juicycleff/RimTalk)
- [RimTalk文档](https://github.com/juicycleff/RimTalk/wiki)

## 💡 提示和技巧

### 1. 开发模式调试

在RimWorld中启用开发模式查看详细日志：
```
选项 → 开发模式 → 勾选
```

### 2. 快速重新加载

修改代码后快速测试：
```powershell
.\build.ps1; echo "按Alt+F4重启游戏"
```

### 3. 热重载（高级）

使用PublicizeAssemblies可以实现某些情况下的热重载（需要额外工具）。

### 4. 性能分析

启用性能分析查看AI调用开销：
```
开发模式 → 性能分析器
```

## 🤝 获取帮助

遇到问题？
1. 📖 查看[完整README](README_CN.md)
2. 🐛 查看[已知问题](#)
3. 💬 在[GitHub Issues](https://github.com/yourusername/RimTalk-Quests/issues)提问
4. 🌐 加入RimWorld模组开发社区

祝你模组开发愉快！🎉
