using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Content.Scripts.Back.Authorization
{
    public class ProfileUI : MonoBehaviour
    {
        private const string PlayerPrefsNameKey = "profile.player_name";

        private Canvas _canvas;
        private RectTransform _root;
        private Button _iconButton;
        private GameObject _panel;

        private Text _nameText;
        private Button _editNameButton;
        private GameObject _nameEditRow;
        private InputField _nameInput;
        private Button _saveNameButton;

        private InputField _oldPassInput;
        private InputField _newPassInput;
        private InputField _confirmPassInput;
        private Button _updatePassButton;
        private Text _statusText;

        private bool _panelVisible;
        private Font _overrideFont;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindObjectOfType<ProfileUI>() != null) return;
            var go = new GameObject("__ProfileUI__");
            DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.DontSave;
            go.AddComponent<ProfileUI>();
        }

        private async void Awake()
        {
            // Шрифт: сперва пробуем Resources/LegacyRuntime, затем встроенный LegacyRuntime.ttf, затем системные (Segoe UI/Arial/Tahoma)
            try { _overrideFont = Resources.Load<Font>("LegacyRuntime"); } catch { _overrideFont = null; }
            if (_overrideFont == null)
            {
                try { _overrideFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { _overrideFont = null; }
            }
            if (_overrideFont == null)
            {
                try { _overrideFont = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Arial", "Tahoma" }, 16); } catch { _overrideFont = null; }
            }

            EnsureEventSystem();
            BuildCanvas();
            BuildIcon();
            BuildPanel();

            _panel.SetActive(false);
            _iconButton.gameObject.SetActive(false);

            await EnsureServicesInitialized();

            AuthenticationService.Instance.SignedIn += OnSignedIn;
            AuthenticationService.Instance.SignedOut += OnSignedOut;

            if (AuthenticationService.Instance.IsSignedIn)
                OnSignedIn();

            SceneManager.sceneLoaded += OnSceneLoaded;

           try { var svc = GameObject.FindObjectOfType<Content.Back.Authorization.UgsAuthService>(); if (svc != null) svc.OnSignedIn += () => { try { _ = RefreshNameAsync(); } catch {} }; } catch {}
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            if (UnityServices.State == ServicesInitializationState.Initialized)
            {
                AuthenticationService.Instance.SignedIn -= OnSignedIn;
                AuthenticationService.Instance.SignedOut -= OnSignedOut;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            try
            {
                var systems = FindObjectsOfType<EventSystem>();
                if (systems.Length == 0)
                {
                    new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                }
            }
            catch { }
        }

        private async Task EnsureServicesInitialized()
        {
            if (UnityServices.State == ServicesInitializationState.Initialized) return;
            try { await UnityServices.InitializeAsync(); } catch { }
        }

        private void EnsureEventSystem()
        {
            var systems = FindObjectsOfType<EventSystem>();
            if (systems.Length == 0)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }
        }

        private void BuildCanvas()
        {
            var canvasGo = new GameObject("ProfileUICanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            DontDestroyOnLoad(canvasGo);
            _canvas = canvasGo.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 3000; // поверх остального UI

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f;

            _root = canvasGo.GetComponent<RectTransform>();
        }

        private void BuildIcon()
        {
            var iconGo = new GameObject("ProfileIcon", typeof(RectTransform), typeof(Image), typeof(Button));
            iconGo.transform.SetParent(_root, false);
            var rt = (RectTransform)iconGo.transform;
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.sizeDelta = new Vector2(80, 80);
            rt.anchoredPosition = new Vector2(-16, -16);

            var img = iconGo.GetComponent<Image>();
            img.color = new Color(0.18f, 0.2f, 0.25f, 0.95f);

            _iconButton = iconGo.GetComponent<Button>();
            _iconButton.onClick.AddListener(TogglePanel);

            var dot = new GameObject("Dot", typeof(RectTransform), typeof(Image));
            dot.transform.SetParent(iconGo.transform, false);
            var drt = (RectTransform)dot.transform;
            drt.anchorMin = new Vector2(0.5f, 0.5f);
            drt.anchorMax = new Vector2(0.5f, 0.5f);
            drt.pivot = new Vector2(0.5f, 0.5f);
            drt.sizeDelta = new Vector2(22, 22);
            var dimg = dot.GetComponent<Image>();
            dimg.color = new Color(0.7f, 0.85f, 1f, 1f);
        }

        private void BuildPanel()
        {
            _panel = new GameObject("ProfilePanel", typeof(RectTransform), typeof(Image));
            _panel.transform.SetParent(_root, false);
            var rt = (RectTransform)_panel.transform;
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.sizeDelta = new Vector2(640, 520);
            rt.anchoredPosition = new Vector2(-16, -104);

            var bg = _panel.GetComponent<Image>();
            bg.color = new Color(0.08f, 0.09f, 0.12f, 0.98f);

            var title = CreateText(_panel.transform, "Профиль", 22, FontStyle.Bold);
            PlaceTop(title, -16, 34);

            var nameRow = new GameObject("NameRow", typeof(RectTransform));
            nameRow.transform.SetParent(_panel.transform, false);
            var nrt = (RectTransform)nameRow.transform;
            nrt.anchorMin = new Vector2(0, 1);
            nrt.anchorMax = new Vector2(1, 1);
            nrt.pivot = new Vector2(0.5f, 1);
            nrt.anchoredPosition = new Vector2(0, -64);
            nrt.sizeDelta = new Vector2(-32, 36);

            _nameText = CreateText(nameRow.transform, "Имя: —", 18, FontStyle.Normal);
            var nt = (RectTransform)_nameText.transform;
            nt.anchorMin = new Vector2(0, 0.5f);
            nt.anchorMax = new Vector2(1, 0.5f);
            nt.pivot = new Vector2(0, 0.5f);
            nt.anchoredPosition = new Vector2(16, 0);
            nt.sizeDelta = new Vector2(-96, 28);
            _nameText.alignment = TextAnchor.MiddleLeft;

            var editGo = new GameObject("EditNameButton", typeof(RectTransform), typeof(Button), typeof(Image));
            editGo.transform.SetParent(nameRow.transform, false);
            var ert = (RectTransform)editGo.transform;
            ert.anchorMin = new Vector2(1, 0.5f);
            ert.anchorMax = new Vector2(1, 0.5f);
            ert.pivot = new Vector2(1, 0.5f);
            ert.sizeDelta = new Vector2(34, 30);
            ert.anchoredPosition = new Vector2(-16, 0);
            var eimg = editGo.GetComponent<Image>();
            eimg.color = new Color(0.22f, 0.27f, 0.35f, 1f);
            _editNameButton = editGo.GetComponent<Button>();
            var editLabel = CreateText(editGo.transform, "✏", 16, FontStyle.Normal);
            editLabel.alignment = TextAnchor.MiddleCenter;

            _nameEditRow = new GameObject("NameEditRow", typeof(RectTransform));
            _nameEditRow.transform.SetParent(_panel.transform, false);
            var nert2 = (RectTransform)_nameEditRow.transform;
            nert2.anchorMin = new Vector2(0, 1);
            nert2.anchorMax = new Vector2(1, 1);
            nert2.pivot = new Vector2(0.5f, 1);
            nert2.anchoredPosition = new Vector2(0, -108);
            nert2.sizeDelta = new Vector2(-32, 40);

            _nameInput = CreateInputField(_nameEditRow.transform, "Новое имя");
            var nir = (RectTransform)_nameInput.transform;
            nir.anchorMin = new Vector2(0, 0.5f);
            nir.anchorMax = new Vector2(1, 0.5f);
            nir.pivot = new Vector2(0, 0.5f);
            nir.anchoredPosition = new Vector2(16, 0);
            nir.sizeDelta = new Vector2(-140, 36);

            _saveNameButton = CreateButton(_nameEditRow.transform, "Сохранить");
            var srt = (RectTransform)_saveNameButton.transform;
            srt.anchorMin = new Vector2(1, 0.5f);
            srt.anchorMax = new Vector2(1, 0.5f);
            srt.pivot = new Vector2(1, 0.5f);
            srt.anchoredPosition = new Vector2(-16, 0);
            srt.sizeDelta = new Vector2(120, 36);

            _nameEditRow.SetActive(false);

            var sep = new GameObject("Sep", typeof(RectTransform), typeof(Image));
            sep.transform.SetParent(_panel.transform, false);
            var srt2 = (RectTransform)sep.transform;
            srt2.anchorMin = new Vector2(0, 1);
            srt2.anchorMax = new Vector2(1, 1);
            srt2.pivot = new Vector2(0.5f, 1);
            srt2.anchoredPosition = new Vector2(0, -156);
            srt2.sizeDelta = new Vector2(-32, 1);
            sep.GetComponent<Image>().color = new Color(1, 1, 1, 0.09f);

            var passTitle = CreateText(_panel.transform, "Смена пароля", 18, FontStyle.Bold);
            PlaceTop(passTitle, -176, 28);

            _oldPassInput = CreateInputField(_panel.transform, "Текущий пароль", true);
            PlaceInput(_oldPassInput, -210);

            _newPassInput = CreateInputField(_panel.transform, "Новый пароль", true);
            PlaceInput(_newPassInput, -252);

            _confirmPassInput = CreateInputField(_panel.transform, "Повторите новый пароль", true);
            PlaceInput(_confirmPassInput, -294);

            _updatePassButton = CreateButton(_panel.transform, "Изменить пароль");
            var upr = (RectTransform)_updatePassButton.transform;
            upr.anchorMin = new Vector2(1, 1);
            upr.anchorMax = new Vector2(1, 1);
            upr.pivot = new Vector2(1, 1);
            upr.anchoredPosition = new Vector2(-16, -340);
            upr.sizeDelta = new Vector2(200, 38);

            _statusText = CreateText(_panel.transform, "", 14, FontStyle.Italic);
            var str = (RectTransform)_statusText.transform;
            str.anchorMin = new Vector2(0, 0);
            str.anchorMax = new Vector2(1, 0);
            str.pivot = new Vector2(0.5f, 0);
            str.anchoredPosition = new Vector2(0, 12);
            str.sizeDelta = new Vector2(-32, 44);
            _statusText.alignment = TextAnchor.LowerLeft;

            void StartEditName()
            {
                if (_nameEditRow != null) _nameEditRow.SetActive(true);
                _nameInput.text = SafeCurrentName();
            }

            string SafeCurrentName()
            {
                var n = _nameText != null ? _nameText.text : null;
                if (!string.IsNullOrEmpty(n) && n.StartsWith("Имя:"))
                {
                    var idx = n.IndexOf(':');
                    if (idx >= 0 && idx + 1 < n.Length)
                        return n.Substring(idx + 1).Trim();
                }
                var cached = PlayerPrefs.GetString(PlayerPrefsNameKey, string.Empty);
                return string.IsNullOrWhiteSpace(cached) ? string.Empty : cached;
            }

            _editNameButton.onClick.AddListener(StartEditName);
            _saveNameButton.onClick.AddListener(() => _ = SaveNameAsync());
            _updatePassButton.onClick.AddListener(() => _ = UpdatePasswordAsync());
        }

        private void PlaceTop(Text t, float y, float h)
        {
            var rt = (RectTransform)t.transform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, y);
            rt.sizeDelta = new Vector2(-32, h);
            t.alignment = TextAnchor.UpperLeft;
            t.color = new Color(0.95f, 0.98f, 1f, 1f);
        }

        private void PlaceInput(InputField field, float y)
        {
            var rt = (RectTransform)field.transform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, y);
            rt.sizeDelta = new Vector2(-32, 36);
        }

        private void TogglePanel()
        {
            _panelVisible = !_panelVisible;
            _panel.SetActive(_panelVisible);
            if (_panelVisible) _ = RefreshNameAsync();
        }

        private async void OnSignedIn()
        {
            _iconButton.gameObject.SetActive(true);
            await RefreshNameAsync();
        }

        private void OnSignedOut()
        {
            _iconButton.gameObject.SetActive(false);
            _panel.SetActive(false);
            _panelVisible = false;
        }

                private async Task RefreshNameAsync()
        {
            try
            {
                var cached = PlayerPrefs.GetString(PlayerPrefsNameKey, string.Empty);
                if (!string.IsNullOrWhiteSpace(cached)) { ApplyName(cached); return; }

                var name = AuthenticationService.Instance.PlayerName;
                if (string.IsNullOrEmpty(name))
                    name = await AuthenticationService.Instance.GetPlayerNameAsync();
                ApplyName(name);
            }
            catch { }
        }

        private void ApplyName(string name)
        {
            if (_nameText != null) _nameText.text = $"Имя: {name}";
            if (!string.IsNullOrWhiteSpace(name))
            {
                PlayerPrefs.SetString(PlayerPrefsNameKey, name);
                PlayerPrefs.Save();
            }
        }

        private async Task SaveNameAsync()
        {
            var newName = _nameInput.text?.Trim();
            if (string.IsNullOrEmpty(newName)) { SetStatus("Введите имя"); return; }
            if (HasWhitespace(newName)) { SetStatus("Имя не должно содержать пробелы"); return; }

            try
            {
                DisableInteractions(true);
                await AuthenticationService.Instance.UpdatePlayerNameAsync(newName);
                var resultName = AuthenticationService.Instance.PlayerName;
                if (string.IsNullOrEmpty(resultName))
                    resultName = await AuthenticationService.Instance.GetPlayerNameAsync();

                ApplyName(resultName);
                SetStatus(resultName != newName
                    ? $"Имя занято, присвоено: {resultName}"
                    : "Имя обновлено");
            }
            catch (AuthenticationException ex) { SetStatus($"Ошибка имени: {ex.Message}"); }
            catch (RequestFailedException ex) { SetStatus($"Сеть/сервер: {ex.Message}"); }
            finally { DisableInteractions(false); }
        }

        private async Task UpdatePasswordAsync()
        {
            var oldPass = _oldPassInput.text;
            var newPass = _newPassInput.text;
            var confirm = _confirmPassInput.text;

            if (string.IsNullOrEmpty(oldPass) || string.IsNullOrEmpty(newPass) || string.IsNullOrEmpty(confirm))
            { SetStatus("Заполните все поля пароля"); return; }

            if (newPass != confirm) { SetStatus("Новые пароли не совпадают"); return; }

            if (!IsPasswordStrong(newPass))
            { SetStatus("Пароль: 8-30 символов, 1 заглавная, 1 строчная, 1 цифра, 1 символ"); return; }

            try
            {
                DisableInteractions(true);
                await AuthenticationService.Instance.UpdatePasswordAsync(oldPass, newPass);
                _oldPassInput.text = _newPassInput.text = _confirmPassInput.text = string.Empty;
                SetStatus("Пароль обновлён");
            }
            catch (AuthenticationException ex) { SetStatus($"Ошибка авторизации: {ex.Message}"); }
            catch (RequestFailedException ex) { SetStatus($"Сеть/сервер: {ex.Message}"); }
            finally { DisableInteractions(false); }
        }

        private void DisableInteractions(bool busy)
        {
            _saveNameButton.interactable = !busy;
            _updatePassButton.interactable = !busy;
            _editNameButton.interactable = !busy;
        }

        private void SetStatus(string msg) { if (_statusText != null) _statusText.text = msg; }

        private static bool HasWhitespace(string s) => Regex.IsMatch(s ?? string.Empty, "\\s");

        private static bool IsPasswordStrong(string s)
        {
            if (string.IsNullOrEmpty(s) || s.Length < 8 || s.Length > 30) return false;
            bool hasLower = false, hasUpper = false, hasDigit = false, hasSymbol = false;
            foreach (var c in s)
            {
                if (char.IsLower(c)) hasLower = true;
                else if (char.IsUpper(c)) hasUpper = true;
                else if (char.IsDigit(c)) hasDigit = true;
                else hasSymbol = true;
            }
            return hasLower && hasUpper && hasDigit && hasSymbol;
        }

        private Text CreateText(Transform parent, string text, int size, FontStyle style)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(Outline));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.text = text;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = new Color(1f, 1f, 1f, 0.97f);
            if (_overrideFont != null) t.font = _overrideFont;

            var outline = go.GetComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.7f);
            outline.effectDistance = new Vector2(1f, -1f);

            return t;
        }

        private InputField CreateInputField(Transform parent, string placeholder, bool isPassword = false)
        {
            var go = new GameObject("InputField", typeof(RectTransform), typeof(Image), typeof(InputField));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = new Color(0.22f, 0.26f, 0.33f, 1f);

            var input = go.GetComponent<InputField>();
            input.contentType = isPassword ? InputField.ContentType.Password : InputField.ContentType.Standard;

            input.textComponent = CreateText(go.transform, string.Empty, 16, FontStyle.Normal);
            input.textComponent.alignment = TextAnchor.MiddleLeft;
            input.textComponent.color = new Color(1, 1, 1, 0.98f);

            var tRT = (RectTransform)input.textComponent.transform;
            tRT.anchorMin = new Vector2(0, 0);
            tRT.anchorMax = new Vector2(1, 1);
            tRT.offsetMin = new Vector2(12, 6);
            tRT.offsetMax = new Vector2(-12, -6);

            var ph = CreateText(go.transform, placeholder, 16, FontStyle.Italic);
            ph.color = new Color(1, 1, 1, 0.6f);
            input.placeholder = ph;
            var pRT = (RectTransform)ph.transform;
            pRT.anchorMin = new Vector2(0, 0);
            pRT.anchorMax = new Vector2(1, 1);
            pRT.offsetMin = new Vector2(12, 6);
            pRT.offsetMax = new Vector2(-12, -6);

            input.caretBlinkRate = 0.75f;
            input.caretColor = new Color32(230, 240, 255, 255);
            input.selectionColor = new Color(0.25f, 0.45f, 0.9f, 0.5f);

            return input;
        }

        private Button CreateButton(Transform parent, string label)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = new Color(0.3f, 0.42f, 0.7f, 1f);

            var btn = go.GetComponent<Button>();
            var txt = CreateText(go.transform, label, 16, FontStyle.Bold);
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;

            var tRT = (RectTransform)txt.transform;
            tRT.anchorMin = new Vector2(0, 0);
            tRT.anchorMax = new Vector2(1, 1);
            tRT.offsetMin = Vector2.zero;
            tRT.offsetMax = Vector2.zero;

            return btn;
        }
    }
}