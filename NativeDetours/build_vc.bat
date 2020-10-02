@echo off

if not defined VCInstallDir (
    echo This script must be called from a Visual Studio developer command prompt.
    echo Press any key to exit...
    pause >nul
    exit
)

pushd Detours
call "%VCInstallDir%\Auxiliary\Build\vcvarsall.bat" x86
set DETOURS_TARGET_PROCESSOR=X86
nmake
pushd lib.X86
cl /D_X86_ /I..\include ..\..\detours.c ..\..\detours.def /LD /Fe:..\..\detours32.dll
popd

call "%VCInstallDir%\Auxiliary\Build\vcvarsall.bat" amd64
set DETOURS_TARGET_PROCESSOR=AMD64
nmake
pushd lib.X64
cl /D_AMD64_ /I..\include ..\..\detours.c ..\..\detours.def /LD /Fe:..\..\detours64.dll
popd
popd
