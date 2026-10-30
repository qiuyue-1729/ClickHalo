# ClickHalo

Windows 鼠标提示工具：提供点击扩散圆盘和跟随光标两种互斥模式，支持六种配色、24–64 像素大小及浅色/深色背景预览。

## 使用

下载 `ClickHalo.exe` 并运行，选择模式、颜色和大小。关闭窗口退出。程序尝试在截图中排除光圈，但不同截图或录屏工具可能不支持这一机制。设置当前不会在退出后保存。

## 构建

需要 Windows 和 .NET Framework 4.x。使用开发者命令提示符或 PowerShell：

```powershell
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /platform:anycpu /out:ClickHalo.exe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll ClickHalo.cs
```

## 致谢与来源

本项目参考了 [CuiLiang 的 ClickShow](https://github.com/cuiliang/ClickShow) 的功能和架构，感谢原作者。ClickHalo 是根据对 ClickShow 程序结构的分析重新编写的实现，并非原仓库的直接 fork 或源码补丁。原项目采用 MIT 许可证，原许可文本见 `NOTICE-ClickShow.txt`。

