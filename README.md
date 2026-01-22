# MagazineFetcher
MagazineFetcher is an automated tool that downloads, extracts, normalizes, classifies, renames, and organizes digital magazines and newspapers.
It is designed for NAS environments (Synology, QNAP, Unraid) but also supports full debugging and development under Windows.

The tool integrates with qBittorrent, processes RSS feeds (e.g., Immortuos), and sorts all downloaded PDFs into a clean, structured library.

## Badges

|W|W|
|---|---|
|Code  | https://dev.azure.com/saigkill/MagazineFetcher |
|Continuous Integration Prod | [![Build Status](https://dev.azure.com/saigkill/MagazineFetcher/_apis/build/status%2FMagazineFetcher-Productive?branchName=master)](https://dev.azure.com/saigkill/MagazineFetcher/_build/latest?definitionId=83&branchName=master)|
|Continuous Integration Dev | [![Build Status](https://dev.azure.com/saigkill/MagazineFetcher/_apis/build/status%2FMagazineFetcher-Stage?branchName=develop)](https://dev.azure.com/saigkill/MagazineFetcher/_build/latest?definitionId=82&branchName=develop) |
|Code Coverage | [![Coverage](https://img.shields.io/azure-devops/coverage/saigkill/MagazineFetcher/82)](https://dev.azure.com/saigkill/MagazineFetcher/_build/latest?definitionId=82) |
|Bugreports|[![GitHub issues](https://img.shields.io/github/issues/saigkill/MagazineFetcher)](https://github.com/saigkill/MagazineFetcher/issues)
|Downloads all|![GitHub Downloads (all assets, all releases)](https://img.shields.io/github/downloads/saigkill/MagazineFetcher/total)|
|Blog|[![Blog](https://img.shields.io/badge/Blog-Saigkill-blue)](https://saschamanns.de)|

File a bug report [on Github](https://github.com/saigkill/MagazineFetcher/issues).

## 🚀Features

* RSS Feed Monitoring  
Automatically detects new magazine or newspaper packs.

* qBittorrent Integration
Adds torrents via the Web API and monitors download progress.

* Smart DownloadWatcher  
Finds the correct download directory based on the title and waits until the download is fully completed.

* Archive Extraction  
Supports ZIP, RAR, and 7z archives — or copies PDFs directly if no archives are present.

* Filename Normalization  
Fixes encoding issues (e.g., Ç¬, ÇÏ, Ç?) and converts them into clean characters (ü, ä, e, etc.).

* Classification & Sorting  
Automatically assigns magazines to target folders based on a configurable mapping.

* Renaming Engine  
Renames files using a customizable pattern such as:
```
{MagazineTitle} - {IssueDate:yyyy-MM} - {IssueNumber:D2}.pdf
```

## 🛠 System Requirements
* .NET 9 Runtime
* qBittorrent with Web UI enabled
* Access to download and target directories
* Optional: Windows network drive mapping for debugging

## 📁 Configuration
Configuration is handled through:
* appsettings.json (base configuration)
* appsettings.Development.json (Windows debugging)
* secrets.json (sensitive data such as passwords)

Example `appsettings.json`
```json
{ 
    "FeedUrl": "https://example.com/rss", 
    "HistoryFile": "/share/Multimedia/apps/MagazineFetcher/history.txt", 
    "TempDirectory": "/share/Multimedia/apps/MagazineFetcher/tmp", 
    "RenamePattern": "{Magazine}-{Issue}-{Year}.pdf", 
    "MagazineMapping": { 
        "Der Spiegel": "/share/Multimedia/Magazines/Der Spiegel", 
        "Focus": "/share/Multimedia/Magazines/Focus", 
        "Für Sie": "/share/Multimedia/Magazines/Für Sie" },
    "QBittorrentClient": { 
        "BaseUrl": "http://192.168.178.30:6363", 
        "Username": "admin", 
        "Password": "xxx", 
        "DownloadPath": "/share/Multimedia/torrent/finished" }, 
    "RssFilter": { 
        "TitleContains": [ "Zeitungen und Magazine", "Magazin Pack" ], 
        "TitleRegex": "Zeitungen und Magazine \\d{2} \\d{2} \\d{4}" } 
}
```

## 🧩 Key Components
* DownloadWatcher
Detects the correct download directory based on the torrent title

* Waits until the directory becomes stable (no size changes)

* ArchiveExtractor
Extracts ZIP/RAR/7z archives

* Copies PDFs directly if no archives are found

* Avoids /tmp overflow on NAS systems

* FileNameNormalizer
Fixes encoding issues from Immortuos

* Converts broken sequences like Ç? into readable characters

* Classifier
Matches magazine names to target folders

* Skips unknown magazines gracefully

* Renamer
Extracts year and issue

* Builds new filenames based on the configured pattern

## 🧪 Debugging on Windows
* Map your NAS directory as a network drive (e.g., M:)

* Create appsettings.Development.json with Windows paths

* Run the project in Visual Studio

Example `appsettings.Development.json`
```json
"HistoryFile": "M:\\apps\\MagazineFetcher\\history.txt", 
"TempDirectory": "M:\\apps\\MagazineFetcher\\tmp", 
"QBittorrentClient:DownloadPath": "M:\\torrent\\finished"
```

## Usage

* Read the [docs](https://moongladesm.blob.core.windows.net/docs/_MagazineFetcher/index.html)

## ▶️ Running the Application
*On Linux (NAS)*
```powershell
dotnet MagazineFetcher.dll
````

*On Windows (Debugging)*
Use Visual Studio → Start Debugging

Folder Structure
```
/Magazines
    /Der Spiegel
        /2026
            Der Spiegel-03-2026.pdf
    /Focus
        /2026
            Focus-02-2026.pdf

/apps/MagazineFetcher
    history.txt
    tmp/
```

## 📝 Logging
* Detailed progress logs
* Error logs for extraction, classification, and renaming
* History prevents reprocessing of old releases

## 🧹 Cleanup
* Temporary directories are removed after processing
* History file is updated automatically
