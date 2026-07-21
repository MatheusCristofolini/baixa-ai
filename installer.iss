; Script de instalação do Inno Setup para o Baixa AI
#define AppName "Baixa AI"
#define AppVersion "1.0.0"
#define AppPublisher "Macritec Tecnologia"
#define AppExeName "BaixaAI.exe"

[Setup]
; Informações básicas do aplicativo
AppId={{D3BC12AB-4A1C-4BC9-93C0-F9A5D6B6E709}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={userpf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes

; Estética do instalador (Limpa e Moderna)
WizardStyle=modern
SetupIconFile=app_icon.ico
UninstallDisplayIcon={app}\app_icon.ico
OutputBaseFilename=Setup_BaixaAI
Compression=lzma2/ultra64
SolidCompression=yes

; Permissões (lowest = Instala no perfil do usuário sem precisar de privilégios de Administrador)
PrivilegesRequired=lowest
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "BaixaAI.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "app_icon.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "yt-dlp.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "ffmpeg.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "ffprobe.exe"; DestDir: "{app}"; Flags: ignoreversion
; OBS: Como ffmpeg.exe, ffprobe.exe e yt-dlp.exe estão no diretório local, o Inno Setup irá empacotá-los.

[Icons]
Name: "{userprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"; IconFilename: "{app}\app_icon.ico"
Name: "{userdesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; IconFilename: "{app}\app_icon.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
