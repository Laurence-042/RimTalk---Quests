# RimTalk-Quests

> **为RimWorld提供动态AI驱动的任务描述生成**

一个RimWorld模组，扩展了[RimTalk](https://github.com/juicycleff/RimTalk)，使用大语言模型生成有上下文感知的AI驱动任务描述。

## 📋 关于

RimTalk-Quests拦截RimWorld的任务生成系统，将静态任务描述替换为由AI生成的动态、叙事丰富的内容。通过复用RimTalk的模型配置和API基础设施，此模组创建了能够适应你殖民地情况的独特任务叙述。

### 归属声明

本模组是基于[juicy](https://github.com/juicycleff/RimTalk)的**RimTalk**的**衍生作品**。  
采用[CC BY-NC-SA 4.0 国际许可协议](http://creativecommons.org/licenses/by-nc-sa/4.0/)授权。

## ✨ 功能特性

- 🎯 **AI生成的任务描述** - 每个任务都具有独特且符合上下文的叙述
- 🔄 **无缝集成** - 使用你现有的RimTalk AI配置
- 🎮 **上下文感知** - 描述根据殖民地状态和任务参数自适应
- 🌐 **多模型支持** - 支持RimTalk支持的所有模型（Google Gemini、OpenAI等）
- ⚡ **轻量级** - 性能影响最小，仅在任务创建时生成描述

## 📦 需求

### 依赖项

1. **[Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077)** - 补丁所需
2. **[RimTalk](https://steamcommunity.com/sharedfiles/filedetails/?id=3378714281)** - 必须安装并配置API密钥

### 兼容性

- RimWorld 1.5+
- RimWorld 1.6+（已测试）

## 🚀 安装

1. 从Steam创意工坊或GitHub安装**Harmony**
2. 安装**RimTalk**并配置你的AI API密钥
3. 安装**RimTalk-Quests**（本模组）
4. 确保加载顺序：`Harmony → RimTalk → RimTalk-Quests`

## ⚙️ 配置

RimTalk-Quests使用你现有的RimTalk设置：
- **API密钥**：在RimTalk设置中配置
- **模型选择**：使用你的RimTalk模型选择（Gemini、OpenAI等）
- **速率限制**：遵守RimTalk的API速率限制

无需额外配置！

### 模组设置

在游戏内的模组设置中，你可以：
- ✅ 启用/禁用AI任务描述
- 💾 启用/禁用描述缓存
- 🗑️ 清除缓存

## 🛠️ 开发

### 从源码构建

```powershell
# 设置RimWorld安装路径（首次运行）
.\setup-env.ps1

# 构建项目
.\build.ps1

# 或者指定版本
.\build.ps1 -GameVersion 1.6 -Configuration Debug
```

### 手动构建

```powershell
# 设置环境变量
$env:RIMWORLD_DIR = "C:\Program Files (x86)\Steam\steamapps\common\RimWorld"

# 构建
dotnet build RimTalkQuests.csproj /p:GameVersion=1.6
```

### 项目结构

```
RimTalk-Quests/
├── About/                    # 模组元数据
│   └── About.xml
├── Source/                   # C#源代码
│   ├── RimTalkQuestsMod.cs              # 主模组类
│   ├── Patches/                          # Harmony补丁
│   │   └── QuestPatches.cs
│   └── Services/                         # 核心服务
│       └── QuestDescriptionGenerator.cs
├── 1.5/Assemblies/           # RimWorld 1.5编译的DLL
├── 1.6/Assemblies/           # RimWorld 1.6编译的DLL
├── build.ps1                 # 构建脚本
├── setup-env.ps1             # 环境设置脚本
└── LICENSE                   # CC BY-NC-SA 4.0许可证
```

### 技术实现

#### 工作原理

1. **任务拦截**：Harmony补丁拦截`Quest.AddToWorld()`方法
2. **上下文构建**：提取任务信息（类型、挑战等级、殖民地状态）
3. **AI调用**：通过反射调用RimTalk的`IAIClient`接口
4. **描述注入**：通过`Quest.get_description`补丁替换原始描述
5. **缓存管理**：可选地缓存生成的描述以提高性能

#### 关键特性

- **异步生成**：描述在后台生成，不阻塞游戏
- **智能缓存**：避免为相同任务重复调用AI API
- **优雅降级**：如果AI服务不可用，使用原始描述
- **反射集成**：通过反射访问RimTalk的内部API，保持松耦合

### 开发路线图

- [x] 基础项目结构和Harmony集成
- [x] RimTalk AI服务集成（通过反射）
- [x] 任务描述生成的上下文构建器
- [x] 缓存系统
- [x] 设置UI
- [ ] 提示工程优化
- [ ] 任务名称AI生成（可选）
- [ ] 本地化支持
- [ ] 性能优化和批处理
- [ ] 高级上下文（殖民地历史、当前事件）

## 🤝 贡献

欢迎贡献！请注意，此项目继承了RimTalk的CC BY-NC-SA 4.0许可证。

### 指南

- 保持代码质量和文档
- 测试多种任务类型
- 遵守API速率限制
- 遵循RimWorld模组开发最佳实践

## 📄 许可证

本作品采用**知识共享 署名-非商业性使用-相同方式共享 4.0 国际许可协议**授权。

- **署名（Attribution）**：基于juicy的RimTalk
- **非商业性使用（NonCommercial）**：不得用于商业用途
- **相同方式共享（ShareAlike）**：衍生作品必须使用相同许可证

详见[LICENSE](LICENSE)文件。

### 第三方许可证

- **RimTalk**：CC BY-NC-SA 4.0 by [juicy](https://github.com/juicycleff/RimTalk)
- **Harmony**：MIT License by [Andreas Pardeike](https://github.com/pardeike/Harmony)
- **RimWorld**：Ludeon Studios专有许可证

## 🐛 已知问题

- 首次运行时需要配置RimTalk的API密钥
- AI生成可能需要几秒钟，在此期间显示原始描述
- 某些复杂任务可能需要更好的提示工程

## 📞 支持

- **问题反馈**：[GitHub Issues](https://github.com/yourusername/RimTalk-Quests/issues)
- **原始RimTalk**：[RimTalk仓库](https://github.com/juicycleff/RimTalk)

## 🙏 致谢

- **juicy** - RimTalk的创建者，本模组基于其工作
- **Andreas Pardeike** - Harmony的创建者
- **Ludeon Studios** - RimWorld
- **RimWorld模组社区** - 文档和支持

## 📝 更新日志

### v1.0.0（初始版本）
- ✨ 通过RimTalk实现AI驱动的任务描述
- 🎯 任务生成的Harmony补丁
- 💾 描述缓存系统
- ⚙️ 游戏内设置UI
- 🔄 与RimTalk AI服务的集成（通过反射）

---

**注意**：此模组需要有效的互联网连接和有效的API密钥（通过RimTalk配置）才能生成任务描述。
