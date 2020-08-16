@echo off
title Dia2Lib Generator

if not defined VSINSTALLDIR (
    echo This script must be called from a Visual Studio developer command prompt.
    echo Press any key to exit...
    pause >nul
    exit
)

if not exist "%VSINSTALLDIR%\DIA SDK" (
    echo The DIA SDK could not be found. Please install the C++ native tools for Visual Studio.
    echo Press any key to exit...
    pause >nul
    exit
)

echo Generating type library...
set DIA=%VSINSTALLDIR%\DIA SDK
midl /I "%DIA%\idl;%DIA%\include" dia2.idl /tlb dia2.tlb /out Dia2Lib

echo Generating Dia2Lib.dll...
tlbimp Dia2Lib\dia2.tlb /out:Dia2Lib\Dia2Lib.dll
