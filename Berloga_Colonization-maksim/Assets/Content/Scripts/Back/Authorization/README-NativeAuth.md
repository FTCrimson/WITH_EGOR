# Native Windows Authentication SDK Integration

Expected native wrapper DLL name: `uaslib_unity.dll`

Place the built DLL here:
`Assets/Plugins/x86_64/uaslib_unity.dll`

Wrapper must export the following C API (C linkage, cdecl):

```
void UAS_SetProjectId(const char* projectId);
void UAS_SetEnvironment(const char* environmentName);
void UAS_SignInAnonymously(void (*cb)(const char* token, const char* playerId, int expiresInSeconds));
void UAS_RefreshToken(void (*cb)(const char* token, const char* playerId, int expiresInSeconds));
void UAS_BeginAutoRefresh(void (*cb)(const char* token, const char* playerId, int expiresInSeconds));
void UAS_EndAutoRefresh();
bool UAS_SessionTokenExists();
void UAS_ClearSessionToken();
const char* UAS_GetToken();
const char* UAS_GetPlayerId();
```

Link the wrapper against the provided `uaslib.lib` (x86_64, Release) from the SDK.

Minimum build settings (Visual Studio):
- Configuration: Release x64
- Runtime: /MD
- C/C++ -> Language: C++17+
- Export functions with `extern "C" __declspec(dllexport)` and `__cdecl` calling convention

After placing the DLL, open Unity; the scene `Assets/Scenes/AuthScene.unity` will be the startup scene. The runtime UI provides buttons:
- "Играть" -> loads `GameScene`
- "Вход" -> triggers anonymous sign-in via native SDK for pipeline validation

Configure `Assets/Resources/AuthConfig.asset` with `ProjectId` and `EnvironmentName`.
