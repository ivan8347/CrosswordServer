FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENV PORT=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "CrosswordServer.dll"]


cd U:\Users\kit\source\repos\CrosswordServer\CrosswordServer
git init


INNO
[Setup]
AppName=NumericCrossword
AppVersion=1.0
DefaultDirName={autopf}\NumericCrossword
DefaultGroupName=NumericCrossword
OutputDir=C:\Users\kit\Desktop\InnoOutput
OutputBaseFilename=Setup_NumericCrossword
Compression=lzma
SolidCompression=yes

[Files]
Source: "C:\Users\kit\Desktop\InstallFiles\NumericCrossword.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\kit\Desktop\InstallFiles\*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\Users\kit\Desktop\InstallFiles\Data\*"; DestDir: "{app}\Data"; Flags: ignoreversion recursesubdirs
Source: "C:\Users\kit\Desktop\InstallFiles\Images\*"; DestDir: "{app}\Images"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\NumericCrossword"; Filename: "{app}\NumericCrossword.exe"
Name: "{autodesktop}\NumericCrossword"; Filename: "{app}\NumericCrossword.exe"

[Run]
Filename: "{app}\NumericCrossword.exe"; Description: "Запустить игру после установки"; Flags: nowait postinstall skipifsilent
