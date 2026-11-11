#ifndef AppVersion
 #define AppVersion "0.8.0"
#endif
#ifndef PayloadDir
 #define PayloadDir "..\dist\payload"
#endif
#define DataFolderName "Görev Yollukları Hesaplama Sistemi"
[Setup]
AppId={{E8CAFB5A-7A38-47D1-B445-1B03B5B52335}
AppName=Görev Yollukları Hesaplama Sistemi
AppVersion={#AppVersion}
VersionInfoVersion={#AppVersion}.0
AppPublisher=Murat ÇALIMLIER
DefaultDirName={localappdata}\Programs\Görev Yollukları Hesaplama Sistemi
DefaultGroupName=Görev Yollukları Hesaplama Sistemi
PrivilegesRequired=lowest
DisableProgramGroupPage=yes
DisableWelcomePage=no
UsePreviousTasks=no
OutputDir=..\dist\installer
OutputBaseFilename=Gorev-Yolluklari-Hesaplama-Sistemi-Kurulum-{#AppVersion}
SetupIconFile=..\src\GorevYolluklari.App\Assets\GorevYolluklari.ico
UninstallDisplayIcon={app}\GorevYolluklari.exe
WizardStyle=modern dark polar includetitlebar hidebevels
WizardBackColor=#10263d
WizardSizePercent=115,115
WizardImageFile=Assets\setup-sidebar.png
WizardSmallImageFile=Assets\setup-mark.png
WizardImageBackColor=#10263d
WizardSmallImageBackColor=#10263d
WizardImageStretch=yes
ShowLanguageDialog=no
#ifdef FastBuild
Compression=zip/1
SolidCompression=no
#else
Compression=lzma2
SolidCompression=yes
#endif
CloseApplications=yes
CloseApplicationsFilter=GorevYolluklari.exe
RestartApplications=no
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"
[Tasks]
Name: "desktopicon"; Description: "Masaüstü kısayolu oluştur"; GroupDescription: "Kısayollar:"
[Files]
Source: "desktop-data.profile"; DestDir: "{app}"; Flags: ignoreversion
; Payload must be freshly published application binaries, never a user workspace.
Source: "{#PayloadDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "Veri\*,Veriler\*,Yedekler\*,Ciktilar\*,Çıktılar\*,PDF\*,Loglar\*,InitialData\*,distribution.profile,*.pdb,*.log"
[Icons]
Name: "{userprograms}\Görev Yollukları Hesaplama Sistemi"; Filename: "{app}\GorevYolluklari.exe"
Name: "{userprograms}\Görev Yollukları Hesaplama Sistemi Kaldır"; Filename: "{uninstallexe}"
Name: "{userdesktop}\Görev Yollukları Hesaplama Sistemi"; Filename: "{app}\GorevYolluklari.exe"; Tasks: desktopicon
[Run]
Filename: "{app}\GorevYolluklari.exe"; Description: "Görev Yollukları Hesaplama Sistemi programını aç"; Flags: nowait postinstall skipifsilent
[Messages]
SetupWindowTitle=Görev Yollukları Hesaplama Sistemi · Kurulum
WelcomeLabel1=Görev Yollukları Hesaplama Sistemi programına hoş geldiniz
WelcomeLabel2=Tüm kayıtlar, ayarlar, belgeler, arşivler ve yedekler masaüstündeki "Görev Yollukları Hesaplama Sistemi" klasöründe tutulur.%n%nNormal kurulum mevcut verileri korur. Temiz kurulum seçeneği programın veri alt klasörlerini siler ve ayrıca onay gerektirir.%n%nSürüm {#AppVersion} · Windows x64
FinishedHeadingLabel=Görev Yollukları Hesaplama Sistemi kullanıma hazır
FinishedLabel=Kurulum tamamlandı.%n%nProgramı Başlat menüsünden veya masaüstü kısayolundan açabilirsiniz.%n%nCreated by Murat ÇALIMLIER
#include "CommonWizard.iss"