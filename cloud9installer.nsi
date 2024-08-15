OutFile "cloud9installer.exe"
InstallDir $APPDATA\cloud9s

!macro CreateInternetShortcut FILEPATH URL
    WriteINIStr "${FILEPATH}" "InternetShortcut" "URL" "${URL}"
!macroend

Section "MainSection" SEC01

    CreateDirectory "$INSTDIR"

    SetOutPath "$INSTDIR"
    File "app.html"
    File "cloud9service.exe"

    Exec '"sc" create "Cloud9" binpath= "$INSTDIR\cloud9service.exe" start= auto'
    Exec '"sc" start "Cloud9"'
    WriteUninstaller "$INSTDIR\uninstall.exe"
    SetShellVarContext all
    !insertmacro CreateInternetShortcut "$Desktop\Cloud9Manager.URL" "http://localhost:4994/"

SectionEnd

Section "Uninstall"
    SetShellVarContext all
    Exec '"sc" stop "Cloud9"'
    Exec '"sc" delete "Cloud9"'
    Delete "$INSTDIR\app.html"
    Delete "$INSTDIR\c9s-*.log"
    Delete "$INSTDIR\S-*.json"
    Delete "$INSTDIR\A-*.json"
    Delete "$INSTDIR\cloud9service.exe"
    Delete "$INSTDIR\uninstall.exe"
    Delete "$Desktop\Cloud9Manager.URL"
    RMDir "$INSTDIR"
SectionEnd