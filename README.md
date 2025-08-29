# UnityCommonSolution-UnityTouchSocket

![GitHub](https://img.shields.io/badge/Unity-2021.3%2B-blue)
![GitHub](https://img.shields.io/badge/license-ApacheLicense2.0-green)
![GitHub](https://img.shields.io/badge/Platform-Windows-red)

此仓库的代码是TouchSocket中适用于Unity部分的代码，TouchSocket由作者若汝棋茗及其他贡献者开发，所有版权归作者若汝棋茗所有，程序集源代码在遵循 Apache License 2.0 的开源协议以及附加协议下，可免费供其他开发者二次开发或（商业）使用。

你可以从以下仓库访问到完整的TouchSocket源代码。  
[Gitee仓库(主库)](https://gitee.com/rrqm_home/touchsocket)  
[Github仓库](https://github.com/RRQM/TouchSocket)  
[Nuget仓库](https://www.nuget.org/profiles/rrqm)  
[入门指南](https://touchsocket.net/)

+ 本项目只是将其中适用于Unity部分单独提取出来，方便在Unity中一步应用。
+ 本项目提取自仓库中的TouchSocketAll-v3.0.19.unitypackage

## 依赖

如果使用克隆方式安装请手动安装以下依赖项。

Unity注册包请在UPM窗口中选择`Unity Registry`项搜索安装；托管包请在UPM中使用`Add git RUL`方式进行安装：

+ Unity InputSystem
+ Unity Newtonsoft-Json
+ Unity Websocket：
  ```bash
   https://github.com/psygames/UnityWebSocket.git#upm 
  ```

## 安装

1. **通过克隆仓库安装**

   将本仓库克隆到您的 Unity 项目的 `Assets` 目录下， 如果使用克隆方式安装，需要<span style="color: #00ff00;">
   手动添加上方的依赖项</span>。

   ```bash
   git clone https://github.com/Pixelsmao/UnityCommonSolution-TouchSocket.git
   ```

2. **使用UPM进行安装：**

   在 Unity 编辑器中，点击顶部菜单栏,打开 Package Manager 窗口.

       Window > Package Manager

   在 Package Manager 窗口的左上角，点击 **+** 按钮，然后选择 **Add package from git URL...**。
   在弹出的输入框中，粘贴本仓库的 Git URL：

       https://github.com/Pixelsmao/UnityCommonSolution-TouchSocket.git

然后点击 **Add**。