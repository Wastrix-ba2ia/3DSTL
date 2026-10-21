#define UNICODE
#define _UNICODE
#include <windows.h>
#include <shobjidl.h>
#include <shlwapi.h>
#include <shellapi.h>
#include <cstdio>
#include <string>
struct Search { DWORD pid; const wchar_t* file; bool found; };
BOOL CALLBACK FindPreview(HWND window, LPARAM data) {
 auto s=(Search*)data;DWORD pid=0;GetWindowThreadProcessId(window,&pid);
 if(s->pid==0||pid==s->pid){wchar_t title[2048]={};GetWindowTextW(window,title,2048);if(wcsstr(title,s->file)&&wcsstr(title,L"3DSTL")){s->found=true;PostMessageW(window,WM_CLOSE,0,0);}}
 return TRUE;
}
int wmain(int argc,wchar_t** argv){
 if(argc!=3)return 2;CoInitializeEx(nullptr,COINIT_APARTMENTTHREADED);
 bool startMenu=wcscmp(argv[1],L"--launch")==0;
 SHELLEXECUTEINFOW launch={sizeof(launch)};launch.fMask=SEE_MASK_NOCLOSEPROCESS|SEE_MASK_FLAG_NO_UI|(startMenu?0:SEE_MASK_CLASSNAME);
 launch.lpClass=startMenu?nullptr:argv[1];launch.lpFile=argv[2];launch.lpVerb=L"open";launch.nShow=SW_SHOW;
 if(!ShellExecuteExW(&launch)){printf("Shell file activation failed: %lu\n",GetLastError());return 6;}
 DWORD pid=launch.hProcess?GetProcessId(launch.hProcess):0;
 if(launch.hProcess)CloseHandle(launch.hProcess);
 Search search={pid,startMenu?L"sample.stl":PathFindFileNameW(argv[2]),false};
 for(int i=0;i<180&&!search.found;i++){EnumWindows(FindPreview,(LPARAM)&search);Sleep(500);}
 CoUninitialize();if(!search.found){printf("No rendered preview window for PID %lu\n",pid);return 7;}
 printf("PASS actual Windows file activation -> rendered preview window, PID %lu\n",pid);return 0;
}
