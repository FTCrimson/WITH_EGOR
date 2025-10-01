using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Content.Back.Authorization
{
    public class AuthService : MonoBehaviour
    {
        public static AuthService Instance { get; private set; }

        public string Token { get; private set; } = string.Empty;
        public string PlayerId { get; private set; } = string.Empty;
        public int ExpiresInSeconds { get; private set; } = 0;

        private AuthConfig _config;
        private NativeAuth.AuthResponseCallback _callbackCache;
        private bool _autoRefreshStarted;
        private bool _nativeReady;

        public bool IsNativeReady => _nativeReady;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _config = Resources.Load<AuthConfig>("AuthConfig");
            if (_config == null)
            {
                Debug.LogWarning("AuthConfig asset not found in Resources. Create it or let the editor auto-creator generate one.");
            }
        }

        public void Initialize(AuthConfig cfg = null)
        {
            _config = cfg != null ? cfg : (_config ?? Resources.Load<AuthConfig>("AuthConfig"));
            if (_config == null)
            {
                Debug.LogError("AuthService: No AuthConfig provided/found.");
                return;
            }

            if (!NativeAuth.IsSupportedPlatform)
            {
                Debug.LogWarning("AuthService: Native Windows SDK is only supported on Windows.");
                return;
            }

            try
            {
                _nativeReady = true;
                NativeAuth.SetProjectId(_config.ProjectId ?? string.Empty);
                NativeAuth.SetEnvironment(_config.EnvironmentName ?? "production");

                _callbackCache = OnAuthResponse;

                if (_config.AutoRefresh && !_autoRefreshStarted)
                {
                    NativeAuth.BeginAutoRefresh(_callbackCache);
                    _autoRefreshStarted = true;
                }
            }
            catch (DllNotFoundException e)
            {
                Debug.LogError($"AuthService: Native DLL not found. Place uaslib_unity.dll into Assets/Plugins/x86_64. {e.Message}");
                _nativeReady = false;
            }
            catch (EntryPointNotFoundException e)
            {
                Debug.LogError($"AuthService: Entry point not found in native DLL. Ensure wrapper exports correct functions. {e.Message}");
                _nativeReady = false;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                _nativeReady = false;
            }
        }

        public void SignInAnonymously()
        {
            if (!_nativeReady)
            {
                Debug.LogWarning("AuthService: Native SDK not ready. Ensure uaslib_unity.dll is placed correctly.");
                return;
            }
            try
            {
                NativeAuth.SignInAnonymously(_callbackCache ?? OnAuthResponse);
            }
            catch (Exception e)
            {
                Debug.LogError($"AuthService.SignInAnonymously failed: {e.Message}");
                _nativeReady = false;
            }
        }

        public void RefreshToken()
        {
            if (!_nativeReady)
            {
                Debug.LogWarning("AuthService: Native SDK not ready. Cannot refresh token.");
                return;
            }
            try
            {
                NativeAuth.RefreshToken(_callbackCache ?? OnAuthResponse);
            }
            catch (Exception e)
            {
                Debug.LogError($"AuthService.RefreshToken failed: {e.Message}");
                _nativeReady = false;
            }
        }

        public void ClearSessionToken()
        {
            try { NativeAuth.ClearSessionToken(); }
            catch (Exception e) { Debug.LogError($"AuthService.ClearSessionToken failed: {e.Message}"); }
        }

        private void OnDestroy()
        {
            try
            {
                if (_autoRefreshStarted)
                {
                    NativeAuth.EndAutoRefresh();
                    _autoRefreshStarted = false;
                }
            }
            catch { /* ignore */ }
        }

        private void OnAuthResponse(string token, string playerId, int expiresIn)
        {
            Token = token ?? string.Empty;
            PlayerId = playerId ?? string.Empty;
            ExpiresInSeconds = expiresIn;
            Debug.Log($"Auth success. PlayerId={PlayerId}, ExpiresIn={ExpiresInSeconds}s");
        }
    }
}
