# 🔧 构建配置更新说明

## ✅ 更新内容

### 1. **正确的路径配置**

已根据实际安装路径更新：
- ✅ **RimWorld**: `D:\SteamLibrary\steamapps\common\RimWorld`
- ✅ **Assembly-CSharp.dll**: `RimWorldWin64_Data\Managed\Assembly-CSharp.dll`
- ✅ **RimTalk.dll**: `steamapps\workshop\content\294100\3551203752\1.6\Assemblies\RimTalk.dll`

### 2. **启用静态类型检查**

通过`UseLocalDlls=true`启用真实DLL引用：
```xml
<PropertyGroup>
    <UseLocalDlls>true</UseLocalDlls>
</PropertyGroup>
```

现在编译时会：
- ✅ 检查RimWorld API的正确使用
- ✅ 检查RimTalk API的正确使用
- ✅ 提供IntelliSense支持
- ✅ 在编译时捕获类型错误

### 3. **智能路径解析**

**setup-env.ps1** 更新：
- 优先检测你的实际路径 `D:\SteamLibrary\...`
- 自动检测常见安装位置

**build.ps1** 更新：
- 自动检测Steam Workshop中的RimTalk
- 回退到本地Mods文件夹
- 显示检测到的DLL路径

**RimTalkQuests.csproj** 更新：
```xml
<!-- 正确计算路径 -->
<SteamAppsDir>$(RimWorldDir)\..\..\ </SteamAppsDir>
<WorkshopRimTalkDll>$(SteamAppsDir)workshop\content\294100\3551203752\$(GameVersion)\Assemblies\RimTalk.dll</WorkshopRimTalkDll>
```

### 4. **移除冗余内容**

简化了build.ps1：
- ❌ 移除了冗长的说明文本
- ❌ 移除了不必要的检查
- ✅ 保留了核心功能
- ✅ 添加了`-UseNuGet`选项用于无本地DLL的构建

## 🚀 使用方法

### 首次设置

```powershell
# 运行环境设置（会自动检测你的路径）
.\setup-env.ps1
```

### 日常构建

```powershell
# 使用本地DLL（推荐，启用静态检查）
.\build.ps1

# 或使用NuGet包（无需RimWorld安装）
.\build.ps1 -UseNuGet
```

### 手动构建

```powershell
# 设置环境变量
$env:RIMWORLD_DIR = 'D:\SteamLibrary\steamapps\common\RimWorld'

# 使用本地DLL构建（静态检查）
dotnet build /p:UseLocalDlls=true

# 使用NuGet包构建（无静态检查）
dotnet build /p:UseLocalDlls=false
```

## 📊 构建模式对比

| 特性 | UseLocalDlls=true | UseLocalDlls=false |
|------|-------------------|---------------------|
| **RimWorld DLL** | ✅ 真实DLL | ❌ NuGet引用 |
| **RimTalk DLL** | ✅ 真实DLL | ❌ 不引用 |
| **静态检查** | ✅ 完整 | ⚠️ 部分 |
| **IntelliSense** | ✅ 完整 | ⚠️ 基础 |
| **编译速度** | 🟢 快 | 🟢 快 |
| **需要安装** | ✅ 需要 | ❌ 不需要 |
| **推荐用途** | 开发 | CI/CD |

## ✅ 验证结果

### 构建输出
```
========================================
  RimTalk-Quests Build Script
========================================

RimWorld Path: D:\SteamLibrary\steamapps\common\RimWorld
RimTalk DLL:   Workshop (Steam) ✓
Game Version:  1.6
Configuration: Debug

Building project...
  RimTalkQuests -> D:\RimMod\Dev\RimTalk-Quests\1.6\Assemblies\RimTalkQuests.dll

已成功生成。
    0 个警告
    0 个错误
```

### 引用确认
- ✅ Assembly-CSharp.dll 正确引用
- ✅ UnityEngine.dll 正确引用
- ✅ RimTalk.dll 正确引用（从Workshop）
- ✅ Harmony 正确引用（从NuGet）
- ✅ 无警告无错误

## 🎯 静态检查的好处

1. **编译时错误检测**
   ```csharp
   // 如果RimTalk API改变，会立即报错
   Services.QuestDescriptionGenerator.IsAIServiceAvailable()
   ```

2. **类型安全**
   ```csharp
   // 编译器会检查Quest类的正确使用
   Quest __instance
   ```

3. **API探索**
   - IntelliSense显示RimTalk的所有公共API
   - 查看方法签名和文档
   - 自动完成

4. **重构支持**
   - 安全地重命名变量
   - 查找所有引用
   - 快速导航到定义

## 📝 路径说明

### RimWorld 路径结构
```
D:\SteamLibrary\steamapps\
├── common\
│   └── RimWorld\                          # RimWorld主目录
│       ├── RimWorldWin64_Data\
│       │   └── Managed\
│       │       └── Assembly-CSharp.dll    # 游戏主DLL
│       └── Mods\                          # 本地模组
│           └── RimTalk-Quests\            # 构建输出
└── workshop\
    └── content\
        └── 294100\                        # RimWorld的Steam App ID
            └── 3551203752\                # RimTalk的Workshop ID
                └── 1.6\
                    └── Assemblies\
                        └── RimTalk.dll    # RimTalk DLL
```

### 项目引用优先级
1. **Workshop位置**（优先）
   - `steamapps/workshop/content/294100/3551203752/1.6/Assemblies/RimTalk.dll`
2. **本地Mods**（回退）
   - `RimWorld/Mods/RimTalk/1.6/Assemblies/RimTalk.dll`

## 🔍 调试技巧

### 检查引用路径
```powershell
# 查看MSBuild详细输出
dotnet build /v:detailed | Select-String -Pattern "RimTalk"
```

### 查看引用的DLL
```powershell
# 使用ILSpy或dotPeek查看编译后的引用
# 或使用命令行
ildasm 1.6\Assemblies\RimTalkQuests.dll
```

### 验证Workshop路径
```powershell
Test-Path 'D:\SteamLibrary\steamapps\workshop\content\294100\3551203752\1.6\Assemblies\RimTalk.dll'
# 应该返回 True
```

## 💡 提示

1. **开发时使用本地DLL** - 获得完整的静态检查
2. **CI/CD使用NuGet** - 无需安装RimWorld
3. **更新RimTalk后** - 重新构建以使用新API
4. **遇到引用错误** - 检查路径是否正确
5. **清理构建** - `Remove-Item obj -Recurse -Force`

## 🎉 总结

现在项目配置为：
- ✅ 使用正确的实际路径
- ✅ 启用完整的静态类型检查
- ✅ 自动检测Steam Workshop中的RimTalk
- ✅ 提供两种构建模式（本地DLL vs NuGet）
- ✅ 简化的构建脚本
- ✅ 无警告无错误的构建

**推荐工作流**：
1. 运行 `.\setup-env.ps1` 一次
2. 日常使用 `.\build.ps1` 构建
3. 享受完整的IntelliSense和静态检查！
