# PDF_Manager
Tested Claude Sonnet 4.6 to its limits — a full WPF app built almost entirely by AI. Creativity no longer needs technical knowledge as a prerequisite. With curiosity, anyone can build. Just never let the AI replace your vision — use it to amplify it.



# Gestore PDF

App Windows per visualizzare, annotare e modificare PDF e immagini. Interfaccia semplice, ottimizzata per penna/touch e tastiera.

![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![Platform](https://img.shields.io/badge/Platform-Windows%2010%2F11%20x64-0078D6?logo=windows)
![License](https://img.shields.io/badge/License-MIT-green)

---

## Funzionalità

- **Visualizzazione PDF** — rendering ad alta qualità via PDFiumCore
- **Annotazioni native** — disegno a mano libera con penna/stilo (salvate come `/Ink` nel PDF), caselle di testo (salvate come `/FreeText`)
- **Multi-documento** — schede multiple, un documento per scheda
- **Gestione pagine** — riordina (drag & drop), ruota, elimina, dividi
- **Combina** — unisci PDF e immagini (JPG, PNG, BMP, TIFF) in un unico PDF
- **Apri immagini** — PNG, JPG, BMP, TIFF, GIF aperti direttamente come PDF
- **PDF cifrati** — supporto password all'apertura
- **Zoom** — Ctrl+rotella mouse, Ctrl+`+`/`-`, Ctrl+`0` per reset
- **Pan** — tasto centrale mouse (sempre), oppure bottone ✋ / tasto `H`
- **Touch & penna** — supporto stilo, InkCanvas nativo WPF
- **Interfaccia italiana**

---

## Requisiti

### Per usare l'app (utente finale)
- Windows 10 / 11 (x64)
- Nessun altro requisito — runtime .NET 8 incluso nell'installer

### Per compilare da sorgente (sviluppatore)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8) o superiore
- Windows 10 / 11 (x64)
- [Inno Setup 6](https://jrsoftware.org/isdl.php) *(solo per creare l'installer)*

---

## Installazione rapida

1. Vai alla sezione [**Releases**](../../releases) di questo repository
2. Scarica `GestorePDF-Setup-x.x.x.exe`
3. Doppio click → installa senza privilegi di amministratore
4. Avvia **Gestore PDF** dall'icona sul desktop

---

## Build da sorgente

```powershell
# 1. Clona il repository
git clone https://github.com/<utente>/PDF_Manager.git
cd PDF_Manager

# 2. Ripristina dipendenze e compila
dotnet build PDF_Manager.sln

# 3. Esegui direttamente (senza installer)
dotnet run --project src/PdfManager.App/PdfManager.App.csproj
```

### Creare l'installer

```powershell
# 1. Genera icona (solo prima volta)
dotnet run --project build/IconGen/IconGen.csproj

# 2. Pubblica app self-contained
dotnet publish src/PdfManager.App/PdfManager.App.csproj `
    -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=false `
    -o artifacts/win-x64

# 3. Compila installer con Inno Setup
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" build\installer.iss
```

Output: `artifacts\installer\GestorePDF-Setup-1.0.0.exe`

---

## Struttura progetto

```
PDF_Manager/
├─ src/
│  ├─ PdfManager.App/        # WPF UI (MVVM, viste, risorse italiane)
│  └─ PdfManager.Core/       # Logica PDF (PDFiumCore, PdfSharp, annotazioni)
├─ src/PdfManager.Tests/     # Test xUnit (annotation round-trip, merge, page ops)
├─ build/
│  ├─ installer.iss          # Script Inno Setup
│  ├─ publish.ps1            # Script di pubblicazione
│  └─ IconGen/               # Generatore icona .NET
└─ assets/icons/app.ico      # Icona app (generata da IconGen)
```

---

## Stack tecnologico

| Componente | Libreria |
|------------|----------|
| UI | WPF .NET 8, CommunityToolkit.Mvvm |
| Rendering PDF | [PDFiumCore](https://github.com/balbarak/pdfium-binaries) |
| Manipolazione PDF | [PdfSharp 6.x](https://github.com/empira/PDFsharp) (MIT) |
| Drag & Drop | gong-wpf-dragdrop |
| Test | xUnit, FluentAssertions |
| Installer | Inno Setup 6 |

---

## Shortcut tastiera

| Tasto | Azione |
|-------|--------|
| `Ctrl+O` | Apri file |
| `Ctrl+S` | Salva |
| `Ctrl+Shift+S` | Salva con nome |
| `Ctrl+W` | Chiudi scheda |
| `Ctrl+Z` / `Ctrl+Y` | Annulla / Ripeti |
| `P` | Attiva penna |
| `T` | Attiva testo |
| `E` | Attiva gomma |
| `H` | Attiva pan (mano) |
| `R` | Ruota pagina |
| `Del` | Elimina pagina |
| `Ctrl++` / `Ctrl+-` | Zoom in / out |
| `Ctrl+0` | Reset zoom 100% |
| `Esc` | Disattiva strumento corrente |

---

## Licenza

MIT — vedi [LICENSE](LICENSE)

---

## Autore

**Davide Silvestri**
