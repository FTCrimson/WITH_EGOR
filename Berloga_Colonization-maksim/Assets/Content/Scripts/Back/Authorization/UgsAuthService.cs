using System;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using Unity.Services.Authentication;

namespace Content.Back.Authorization
{
    public class UgsAuthService : MonoBehaviour
    {
        public static UgsAuthService Instance { get; private set; }

        public bool Initialized { get; private set; }
        public string PlayerId => AuthenticationService.Instance.IsSignedIn ? AuthenticationService.Instance.PlayerId : string.Empty;
        public string AccessToken => AuthenticationService.Instance.IsSignedIn ? AuthenticationService.Instance.AccessToken : string.Empty;

        private AuthConfig _config;

        public event System.Action OnSignedIn;
        public event System.Action<string> OnError;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public async void Initialize(AuthConfig cfg = null)
        {
            _config = cfg != null ? cfg : (_config ?? Resources.Load<AuthConfig>("AuthConfig"));
            if (_config == null)
            {
                Debug.LogError("UgsAuthService: No AuthConfig found.");
                return;
            }

            if (Initialized) return;
            try
            {
                var options = new InitializationOptions();
                if (!string.IsNullOrEmpty(_config.EnvironmentName))
                {
                    options.SetEnvironmentName(_config.EnvironmentName);
                }

                await UnityServices.InitializeAsync(options);
                Initialized = true;
                Debug.Log("UGS initialized");

                AuthenticationService.Instance.SignedIn += () =>
                {
                    Debug.Log($"UGS SignedIn. PlayerId={AuthenticationService.Instance.PlayerId}");
                    OnSignedIn?.Invoke();
                };
                AuthenticationService.Instance.SignedOut += () =>
                {
                    Debug.Log("UGS SignedOut");
                };
                AuthenticationService.Instance.Expired += () =>
                {
                    Debug.LogWarning("UGS Session expired");
                };

                if (AuthenticationService.Instance.IsSignedIn)
                {
                    OnSignedIn?.Invoke();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"UGS Initialize failed: {e.Message}");
                OnError?.Invoke($"UGS init failed: {e.Message}");
            }
        }

        public async Task<bool> RegisterAsync(string usernameOrEmail, string password)
        {
            if (!Initialized)
            {
                Debug.LogWarning("UGS not initialized.");
                OnError?.Invoke("UGS не инициализирован");
                return false;
            }
            try
            {
                // Регистрация по имени пользователя/нику
                await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(usernameOrEmail, password);
                Debug.Log("Registration success");

                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(usernameOrEmail, password);
                }

                await EnsurePlayerNameAsync(usernameOrEmail);
                OnSignedIn?.Invoke();
                return true;
            }
            catch (RequestFailedException e)
            {
                Debug.LogError($"Registration failed: {e.ErrorCode} {e.Message}");
                OnError?.Invoke(e.Message);
            }
            catch (Exception e)
            {
                Debug.LogError($"Registration failed: {e.Message}");
                OnError?.Invoke(e.Message);
            }
            return false;
        }

        public async Task<bool> SignInAsync(string usernameOrEmail, string password)
        {
            if (!Initialized)
            {
                Debug.LogWarning("UGS not initialized.");
                OnError?.Invoke("UGS не инициализирован");
                return false;
            }
            try
            {
                if (AuthenticationService.Instance.IsSignedIn)
                {
                    Debug.Log("Already signed in");
                    OnSignedIn?.Invoke();
                    return true;
                }
                await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(usernameOrEmail, password);
                await EnsurePlayerNameAsync(usernameOrEmail);
                OnSignedIn?.Invoke();
                Debug.Log($"SignIn success. PlayerId={PlayerId}");
                return true;
            }
            catch (RequestFailedException e)
            {
                Debug.LogError($"SignIn failed: {e.ErrorCode} {e.Message}");
                OnError?.Invoke(e.Message);
            }
            catch (Exception e)
            {
                Debug.LogError($"SignIn failed: {e.Message}");
                OnError?.Invoke(e.Message);
            }
            return false;
        }

        public void SignOut()
        {
            if (!Initialized) return;
            if (AuthenticationService.Instance.IsSignedIn)
            {
                AuthenticationService.Instance.SignOut();
            }
        }

        private static string SanitizePreferredName(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            var s = input.Trim();
            var at = s.IndexOf('@');
            if (at > 0) s = s.Substring(0, at);
            s = Regex.Replace(s, "[^A-Za-z0-9._-]", "");
            if (s.Length > 20) s = s.Substring(0, 20);
            if (s.Length < 3) return null;
            return s;
        }

        private static bool LooksRandomDefault(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return true;
            var lower = name.ToLowerInvariant();
            if (lower.StartsWith("player")) return true;
            if (Regex.IsMatch(lower, "^user[-_]?\\d+$")) return true;
            return false;
        }

        private async Task EnsurePlayerNameAsync(string preferred)
        {
            try
            {
                var current = AuthenticationService.Instance.PlayerName;
                try { if (string.IsNullOrEmpty(current)) current = await AuthenticationService.Instance.GetPlayerNameAsync(); } catch { }

                if (string.IsNullOrEmpty(current) || LooksRandomDefault(current))
                {
                    var sanitized = SanitizePreferredName(preferred);
                    if (!string.IsNullOrEmpty(sanitized))
                    {
                        try { await AuthenticationService.Instance.UpdatePlayerNameAsync(sanitized); }
                        catch (RequestFailedException e) { Debug.LogWarning($"Update name failed: {e.ErrorCode} {e.Message}"); }
                        catch (Exception e) { Debug.LogWarning($"Update name failed: {e.Message}"); }
                    }
                }
            }
            catch { }
        }
    }
}

