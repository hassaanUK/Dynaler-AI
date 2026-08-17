# Dynaler AI

AI-powered Windows desktop controller built with C# and WPF.

---

## Download

Pre-built EXE on the [Releases page](https://github.com/hassaanUK/Dynaler-AI/releases).

---

## Running from Source

### Requirements
- Windows 10/11
- .NET 8 SDK: https://dotnet.microsoft.com/download/dotnet/8.0
- Visual Studio 2022 or dotnet CLI

### Build and Run

  git clone https://github.com/hassaanUK/Dynaler-AI.git
  cd Dynaler-AI/DynalerAI
  dotnet run

### Build EXE

  dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

Output: bin/Release/net8.0-windows/win-x64/publish/DynalerAI.exe

---

## Features

- Built-in AI (ChatGPT / GPT-4o)
- Custom API Key - OpenAI, Anthropic Claude, or Google Gemini
- Screen Vision
- Safe Mode
- Multi-step Plan View
- Auto-retry
- Task Presets
- Stop Hotkey: Ctrl+Shift+S
- History Log
- Settings Page

---

## Troubleshooting

| Problem | Fix |
|---|---|
| App won't start | Install .NET 8 SDK |
| AI not responding | Check API key in Settings |
| Actions not working | Run as Administrator |
| Build fails | Run dotnet restore first |

---

## License

MIT
