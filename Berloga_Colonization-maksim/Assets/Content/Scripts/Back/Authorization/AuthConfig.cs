using UnityEngine;

namespace Content.Back.Authorization
{
    [CreateAssetMenu(fileName = "AuthConfig", menuName = "Config/Auth Config", order = 0)]
    public class AuthConfig : ScriptableObject
    {
        [Header("Unity Authentication")] public string ProjectId = "";
        public string EnvironmentName = "production";
        public bool AutoRefresh = true;

        [Header("Integration")]
        public bool UseNativeWindowsSDK = true; // Requires native DLL wrapper
        public bool UseUnityAuthentication = true; // UGS Authentication (Username/Password)
    }
}
