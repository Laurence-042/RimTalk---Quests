default: build

build:
    dotnet build RimTalkQuests.csproj -c Debug

# 构建正式版并打包到兄弟目录 ../RimTalk-Quests.release（仓库外，不含源码/.git 等）。
release:
    dotnet build RimTalkQuests.csproj -c Release
    rm -rf ../RimTalk-Quests.release
    mkdir -p ../RimTalk-Quests.release/About
    cp About/About.xml About/Preview.png About/PublishedFileId.txt ../RimTalk-Quests.release/About/
    cp -r 1.6 ../RimTalk-Quests.release/1.6
    rm -f ../RimTalk-Quests.release/1.6/Assemblies/*.pdb
    cp -r Languages ../RimTalk-Quests.release/Languages
    cp LICENSE ../RimTalk-Quests.release/
