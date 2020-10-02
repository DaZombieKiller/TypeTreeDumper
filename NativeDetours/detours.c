#include <Windows.h>
#include "detours.h"
#pragma comment(lib, "detours.lib")

BOOL WINAPI DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpReserved)
{
    return TRUE;
}
