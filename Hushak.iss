#define MyAppName "Hushak"
#define MyAppVersion "1.5.15"
#define MyAppPublisher "Թարումեան ծրագրեր"
#define MyAppPublisherEn "Tarumian programs"
#define MyAppExeName "Hushak.exe"
#define MyPublisherURL "https://tarumian.am"
#define MyAppURL "https://tarumian.am/hushak/"

[Setup]
AppId={{D1A39C25-9D22-4D39-8472-875BC31F58A0}
AppName={cm:MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={cm:MyAppPublisher}
AppPublisherURL={#MyPublisherURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\Hushak
DisableProgramGroupPage=yes
DefaultGroupName={cm:MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}
OutputDir=publish\installer
OutputBaseFilename=Hushak_Setup_v{#MyAppVersion}
SetupIconFile=Assets\Hushak.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
AppMutex=Local\Hushak_SingleInstance_Mutex
CloseApplications=yes
RestartApplications=yes
VersionInfoVersion={#MyAppVersion}.0
VersionInfoCompany={#MyAppPublisherEn}
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}
VersionInfoDescription={#MyAppName} Installer
VersionInfoCopyright=Copyright (C) 2026 Ruben Tarumian

[Languages]
Name: "armenian"; MessagesFile: "compiler:Languages\Armenian.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "french"; MessagesFile: "compiler:Languages\French.isl"
Name: "german"; MessagesFile: "compiler:Languages\German.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"
Name: "portuguese"; MessagesFile: "compiler:Languages\Portuguese.isl"
Name: "arabic"; MessagesFile: "compiler:Languages\Arabic.isl"

[Messages]
armenian.SetupAppTitle=Տեղադրում
armenian.SetupWindowTitle=%1-ի տեղադրում
armenian.UninstallAppTitle=Ապատեղադրում
armenian.UninstallAppFullTitle=%1-ի ապատեղադրում
armenian.SetupLdrStartupMessage=Այս ծրագիրը կտեղադրի %1-ը ձեր համակարգչում։ Շարունակե՞լ։
armenian.LdrCannotCreateTemp=Հնարավոր չէ ստեղծել ժամանակավոր նիշք։ Տեղադրումը կասեցված է
armenian.LdrCannotExecTemp=Հնարավոր չէ կատարել նիշքը ժամանակավոր պանակից։ Տեղադրումը կասեցված է
armenian.SetupFileMissing=%1 նիշքը բացակայում է տեղադրման պանակից։ Ուղղեք խնդիրը կամ ստացեք ծրագրի նոր վարկածը։
armenian.SetupFileCorrupt=Տեղադրվող նիշքերը վնասված են։
armenian.SetupFileCorruptOrWrongVer=Տեղադրվող նիշքերը վնասված են կամ անհամատեղելի են տեղադրիչի այս վարկածի հետ։ Ուղղեք խնդիրը կամ ստացեք ծրագրի նոր վարկածը։
armenian.SetupAlreadyRunning=Տեղադրիչն արդեն աշխատեցված է։
armenian.SetupAppRunningError=Տեղադրիչը հայտնաբերել է, որ %1-ն աշխատում է։%n%nՓակեք այն և սեղմեք «Լավ»՝ շարունակելու համար կամ «Չեղարկել»՝ փակելու համար։
armenian.UninstallAppRunningError=Ապատեղադրիչը հայտնաբերել է, որ %1-ն աշխատում է։%n%nՓակեք այն և սեղմեք «Լավ»՝ շարունակելու համար կամ «Չեղարկել»՝ փակելու համար։
armenian.PrivilegesRequiredOverrideTitle=Ընտրեք տեղադրիչի տեղադրման կերպը
armenian.SelectLanguageTitle=Ընտրել տեղադրիչի լեզուն
armenian.ClickNext=Սեղմեք «Հաջորդ»՝ շարունակելու համար կամ «Չեղարկել»՝ տեղադրիչը փակելու համար։
armenian.ClickFinish=Սեղմեք «Ավարտել»՝ տեղադրիչը փակելու համար։
armenian.WelcomeLabel1=Ձեզ ողջունում է [name]-ի տեղադրման օգնականը
armenian.SelectComponentsDesc=Ո՞ր նիշքերը պետք է տեղադրվեն։
armenian.SelectComponentsLabel2=Նշեք այն նիշքերը, որոնք պետք է տեղադրվեն, ապանշեք նրանք, որոնք չպետք է տեղադրել։ Սեղմեք «Հաջորդ»՝ շարունակելու համար։
armenian.NoUninstallWarningTitle=Տեղադրվող նիշքերը
armenian.NoUninstallWarning=Տեղադրիչ ծրագիրը հայտնաբերել է, որ հետևյալ բաղադրիչները արդեն տեղադրված են Ձեր համակարգչում։ %n%n%1%n%nԱյս բաղադրիչների ընտրության վերակայումը չի ջնջի դրանք։%n%nՇարունակե՞լ։
armenian.ReadyLabel1=Տեղադրիչը պատրաստ է սկսել [name]-ի տեղադրումը։
armenian.DownloadingLabel2=Նիշքերի ներբեռնում...
armenian.ErrorFileSize=Նիշքի անվավեր չափ. ակնկալվում էր %1, գտնվել է %2
armenian.ExtractingLabel=Նիշքերը դուրս են բերվում...
armenian.PreparingDesc=Տեղադրիչը պատրաստվում է տեղադրել [name]-ը ձեր համակարգչում։
armenian.ApplicationsFound=Հետևյալ ծրագրերը օգտագործում են նիշքեր, որոնք պետք է թարմացվեն տեղադրիչի կողմից։ Թույլատրեք տեղադրիչին ինքնաբար փակելու այդ ծրագրերը։
armenian.ApplicationsFound2=Հետևյալ ծրագրերը օգտագործում են նիշքեր, որոնք պետք է թարմացվեն տեղադրիչի կողմից։ Թույլատրեք տեղադրիչին ինքնաբար փակելու այդ ծրագրերը։ Տեղադրումը ավարտելուց հետո տեղադրիչը կփորձի վերամեկնարկել այդ ծրագրերը։
armenian.ErrorCloseApplications=Տեղադրիչը չկարողացավ ինքնաբար փակել բոլոր ծրագրերը: Խորհուրդ ենք տալիս փակել այն բոլոր ծրագրերը, որոնք պետք է թարմացվեն տեղադրիչի կողմից:
armenian.PrepareToInstallNeedsRestart=Տեղադրիչը պետք է վերամեկնարկի ձեր համակարգիչը: Դրանից հետո կրկին աշխատեցրեք այն՝ ավարտելու համար [name]-ի տեղադրումը:%n%nՑանկանո՞ւմ եք վերամեկնարկել հիմա:
armenian.ShowReadmeCheck=Այո, ես ցանկանում եմ նայել README նիշքը։
armenian.ChangeDiskTitle=Տեղադրիչը պահանջում է հաջորդ սկավառակը
armenian.SelectDiskLabel2=Զետեղեք %1 սկավառակը և սեղմեք «Լավ»։ %n%nԵթե նիշքերի պանակը գտնվում է այլ տեղ, ապա ընտրեք ճիշտ ուղին կամ սեղմեք «Ընտրել»։
armenian.FileNotInDir2="%1" նիշքը չի գտնվել "%2"-ում։ Զետեղեք ճիշտ սկավառակ կամ ընտրեք այլ պանակ։
armenian.StatusExtractFiles=Նիշքերի դուրս բերում...
armenian.StatusDownloadFiles=Նիշքերի դուրս բերում...
armenian.StatusRegisterFiles=Նիշքերի գրանցում...
armenian.StatusCreateIniEntries=INI նիշքերի ստեղծում...
armenian.ErrorExecutingProgram=Հնարավոր չէ կատարել %n%1 նիշքը
armenian.ErrorIniEntry=Սխալ՝ "%1" INI նիշքում գրառում կատարելիս։
armenian.FileAbortRetryIgnoreSkipNotRecommended=Բաց թողնել այս նիշքը (խորհուրդ չի տրվում)
armenian.SourceIsCorrupted=Սկզբնական նիշքը վնասված է։
armenian.SourceDoesntExist=Սկզբնական "%1" նիշքը գոյություն չունի
armenian.SourceVerificationFailed=Սկզբնական նիշքի ստուգումը ձախողվեց. %1
armenian.VerificationSignatureDoesntExist="%1" ստորագրության նիշքը գոյության չունի
armenian.VerificationSignatureInvalid=Ստորագրության "%1" նիշքն անվավեր է
armenian.VerificationKeyNotFound=Ստորագրության "%1" նիշքն օգտագործում է անհայտ բանալի
armenian.VerificationFileNameIncorrect=Նիշքի անունը սխալ է
armenian.VerificationFileTagIncorrect=Նիշքի պիտակը սխալ է
armenian.VerificationFileSizeIncorrect=Նիշքի չափը սխալ է
armenian.VerificationFileHashIncorrect=Նիշքի hash-ը սխալ է
armenian.ExistingFileReadOnly2=Առկա նիշքը չի կարող փոխարինվել, քանի որ այն նշված է որպես միայն կարդալու:
armenian.ExistingFileReadOnlyKeepExisting=&Պահել առկա նիշքը
armenian.ErrorReadingExistingDest=Սխալ՝ նիշքը կարդալիս.
armenian.FileExists2=Նիշքը գոյություն չունի
armenian.FileExistsOverwriteExisting=&Վրագրել առկա նիշքը
armenian.FileExistsKeepExisting=&Պահել առկա նիշքը
armenian.ExistingFileNewer2=Առկա նիշքը ավելի նոր է, քան այն, որ տեղադրիչը փորձում է տեղադրել:
armenian.ExistingFileNewerOverwriteExisting=&Վրագրել առկա նիշքը
armenian.ExistingFileNewerKeepExisting=&Պահել առկա նիշքը (հանձնարարելի)
armenian.ErrorChangingAttr=Սխալ՝ ընթացիկ նիշքի հատկանիշները փոխելիս.
armenian.ErrorCreatingTemp=Սխալ՝ նշված պանակում նիշք ստեղծելիս.
armenian.ErrorReadingSource=Սխալ՝ նիշքը կարդալիս.
armenian.ErrorCopying=Սխալ՝ նիշքը պատճենելիս.
armenian.ErrorDownloading=Սխալ՝ նիշքը ներբեռնելու փորձի ժամանակ.
armenian.ErrorReplacingExistingFile=Սխալ՝ գոյություն ունեցող նիշքը փոխարինելիս.
armenian.ErrorRenamingTemp=Սխալ՝ նպատակակետ պանակում՝ նիշքը վերանվանելիս.
armenian.ErrorOpeningReadme=Սխալ՝ README նիշքը բացելիս։
armenian.UninstallNotFound="%1" նիշքը գոյություն չունի։ Հնարավոր չէ ապատեղադրել։
armenian.UninstallOpenError="%1" նիշքը հնարավոր չէ բացել: Հնարավոր չէ ապատեղադրել
armenian.UninstallUnsupportedVer=Ապատեղադրելու "%1" մատյանի նիշքը անճանաչելի է ապատեղադրող ծրագրի այս վարկածի համար։ Հնարավոր չէ ապատեղադրել
armenian.UninstallDataCorrupted="%1" նիշքը վնասված է։ Հնարավոր չէ ապատեղադրել
armenian.ConfirmDeleteSharedFileTitle=Հեռացնե՞լ համատեղ օգտագործվող նիշքը։
armenian.ConfirmDeleteSharedFile2=Համակարգը նշում է, որ հետևյալ համատեղ օգտագործվող նիշքը այլևս չի օգտագործվում այլ ծրագրի կողմից։ Ապատեղադրե՞լ այն։ %n%nԵթե համոզված չեք սեղմեք «Ոչ»։
armenian.SharedFileNameLabel=Նիշքի անուն՝

[CustomMessages]
armenian.MyAppName=Հուշակ
armenian.MyAppPublisher=Թարումեան ծրագրեր
armenian.StartupTaskDesc=Գործարկել Windows-ի մեկնարկի հետ
armenian.DesktopContextMenuDesc=Ավելացնել աշխատասեղանի աջ կտտոցի (տեղային) ցանկում
armenian.ContextMenuAction=Նոր թերթիկ (Հուշակ)

english.MyAppName=Hushak
english.MyAppPublisher=Tarumian programs
english.StartupTaskDesc=Run at Windows startup
english.DesktopContextMenuDesc=Add to Desktop context menu
english.ContextMenuAction=New Note (Hushak)

russian.MyAppName=Hushak
russian.MyAppPublisher=Tarumian programs
russian.StartupTaskDesc=Запускать при старте Windows
russian.DesktopContextMenuDesc=Добавить в контекстное меню рабочего стола
russian.ContextMenuAction=Новая записка (Hushak)

french.MyAppName=Hushak
french.MyAppPublisher=Tarumian programs
french.StartupTaskDesc=Lancer au démarrage de Windows
french.DesktopContextMenuDesc=Ajouter au menu contextuel du bureau
french.ContextMenuAction=Nouvelle note (Hushak)

german.MyAppName=Hushak
german.MyAppPublisher=Tarumian programs
german.StartupTaskDesc=Beim Windows-Start ausführen
german.DesktopContextMenuDesc=Zum Desktop-Kontextmenü hinzufügen
german.ContextMenuAction=Neue Notiz (Hushak)

spanish.MyAppName=Hushak
spanish.MyAppPublisher=Tarumian programs
spanish.StartupTaskDesc=Iniciar con Windows
spanish.DesktopContextMenuDesc=Agregar al menú contextual del escritorio
spanish.ContextMenuAction=Nueva nota (Hushak)

italian.MyAppName=Hushak
italian.MyAppPublisher=Tarumian programs
italian.StartupTaskDesc=Avvia con Windows
italian.DesktopContextMenuDesc=Aggiungi al menu contestuale del desktop
italian.ContextMenuAction=Nuova nota (Hushak)

portuguese.MyAppName=Hushak
portuguese.MyAppPublisher=Tarumian programs
portuguese.StartupTaskDesc=Iniciar com o Windows
portuguese.DesktopContextMenuDesc=Adicionar ao menu de contexto da área de trabalho
portuguese.ContextMenuAction=Nova nota (Hushak)

arabic.MyAppName=Hushak
arabic.MyAppPublisher=Tarumian programs
arabic.StartupTaskDesc=التشغيل عند بدء Windows
arabic.DesktopContextMenuDesc=إضافة إلى قائمة سياق سطح المكتب
arabic.ContextMenuAction=ملاحظة جديدة (Hushak)

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startupicon"; Description: "{cm:StartupTaskDesc}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "desktopcontext"; Description: "{cm:DesktopContextMenuDesc}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "publish\standalone\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "Assets\Hushak.ico"; DestDir: "{app}\Assets"; Flags: ignoreversion
Source: "LICENSE"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{cm:MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{cm:MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon; IconFilename: "{app}\{#MyAppExeName}"
Name: "{userstartup}\{cm:MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: startupicon; IconFilename: "{app}\{#MyAppExeName}"

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "Hushak"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: startupicon
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: none; ValueName: "DesktopNotes"; Flags: deletevalue

; Desktop Background Context Menu (Աշխատասեղանի աջ կտտոցի ցանկ)
Root: HKCU; Subkey: "Software\Classes\DesktopBackground\Shell\Hushak"; ValueType: string; ValueName: ""; ValueData: "{cm:ContextMenuAction}"; Flags: uninsdeletekey; Tasks: desktopcontext
Root: HKCU; Subkey: "Software\Classes\DesktopBackground\Shell\Hushak"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\{#MyAppExeName}"",0"; Flags: uninsdeletekey; Tasks: desktopcontext
Root: HKCU; Subkey: "Software\Classes\DesktopBackground\Shell\Hushak\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" --new-note"; Flags: uninsdeletekey; Tasks: desktopcontext

; Explorer Background Context Menu (Ցանկացած պանակի ֆոնին աջ կտտոցով)
Root: HKCU; Subkey: "Software\Classes\Directory\Background\shell\Hushak"; ValueType: string; ValueName: ""; ValueData: "{cm:ContextMenuAction}"; Flags: uninsdeletekey; Tasks: desktopcontext
Root: HKCU; Subkey: "Software\Classes\Directory\Background\shell\Hushak"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\{#MyAppExeName}"",0"; Flags: uninsdeletekey; Tasks: desktopcontext
Root: HKCU; Subkey: "Software\Classes\Directory\Background\shell\Hushak\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" --new-note"; Flags: uninsdeletekey; Tasks: desktopcontext

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{cm:MyAppName}}"; Flags: nowait postinstall skipifsilent
