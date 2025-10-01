using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Content.Back.Authorization
{
    public class AuthBootstrap : MonoBehaviour
    {
        [SerializeField] private string gameSceneName = "GameScene";

        private void Start()
        {
            EnsureAuthService();
            BuildSimpleUI();
        }

        private void EnsureAuthService()
        {
            if (UgsAuthService.Instance == null)
            {
                var ugs = new GameObject("UgsAuthService");
                ugs.AddComponent<UgsAuthService>().Initialize();
            }
            else
            {
                UgsAuthService.Instance.Initialize();
            }
            // Подписываемся на события авторизации: переход в сцену и показ ошибок
            if (UgsAuthService.Instance != null)
            {
                UgsAuthService.Instance.OnSignedIn += OnUgsSignedIn;
                UgsAuthService.Instance.OnError += OnUgsError;
            }

            // Гарантируем наличие камеры, чтобы не появлялось предупреждение "No Cameras Rendering"
            if (Camera.main == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                var cam = camGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black; // убираем серый фон
            }
            else
            {
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
                Camera.main.backgroundColor = Color.black;
            }
        }

        private Button _playButton;

        private GameObject _mainPanel;
        private GameObject _accountPanel;
        private InputField _emailInput;
        private InputField _passwordInput;
        private Text _statusText;

        private void BuildSimpleUI()
        {
            // Canvas
            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();

            // EventSystem
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // Контейнер с кнопками
            _mainPanel = new GameObject("MainPanel");
            _mainPanel.transform.SetParent(canvasGo.transform, false);
            var panelRect = _mainPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(400, 200);
            panelRect.anchoredPosition = Vector2.zero;

            // Create two buttons: Play and Login
            _playButton = CreateButton(_mainPanel.transform, "Играть", new Vector2(0, 40), OnPlayClicked);
            CreateButton(_mainPanel.transform, "Вход", new Vector2(0, -40), OnLoginClicked);

            // Отключаем кнопку "Играть", если целевая сцена не добавлена в Build Settings
            if (!Application.CanStreamedLevelBeLoaded(gameSceneName))
            {
                if (_playButton != null)
                {
                    _playButton.interactable = false;
                }
                Debug.LogWarning($"AuthBootstrap: Scene '{gameSceneName}' is not in Build Settings. Add it via File -> Build Settings.");
            }

            // Панель аккаунта (вход/регистрация): слева форма, справа арт
            _accountPanel = new GameObject("AccountPanel");
            _accountPanel.transform.SetParent(canvasGo.transform, false);
            var accRect = _accountPanel.AddComponent<RectTransform>();
            accRect.anchorMin = new Vector2(0f, 0f);
            accRect.anchorMax = new Vector2(1f, 1f);
            accRect.offsetMin = new Vector2(40, 40);
            accRect.offsetMax = new Vector2(-40, -40);

            // Контейнер разбиения
            var split = new GameObject("SplitRoot");
            split.transform.SetParent(_accountPanel.transform, false);
            var splitRect = split.AddComponent<RectTransform>();
            splitRect.anchorMin = new Vector2(0f, 0f);
            splitRect.anchorMax = new Vector2(1f, 1f);
            splitRect.offsetMin = Vector2.zero;
            splitRect.offsetMax = Vector2.zero;

            var hlg = split.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 24f;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.padding = new RectOffset(12, 12, 12, 12);

            // Левая колонка: форма
            var formCol = new GameObject("FormColumn");
            formCol.transform.SetParent(split.transform, false);
            var formColRect = formCol.AddComponent<RectTransform>();
            var formColLE = formCol.AddComponent<LayoutElement>();
            formColLE.minWidth = 360;
            formColLE.preferredWidth = 420;
            formColLE.flexibleWidth = 0;

            var formBG = formCol.AddComponent<Image>();
            formBG.color = new Color(0.08f, 0.08f, 0.08f, 0.6f); // легкая подложка

            var form = new GameObject("Form");
            form.transform.SetParent(formCol.transform, false);
            var formRect = form.AddComponent<RectTransform>();
            formRect.anchorMin = new Vector2(0f, 0f);
            formRect.anchorMax = new Vector2(1f, 1f);
            formRect.offsetMin = new Vector2(12, 12);
            formRect.offsetMax = new Vector2(-12, -12);

            var layout = form.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.padding = new RectOffset(6, 6, 6, 6);

            // Заголовок формы
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(form.transform, false);
            var title = titleGo.AddComponent<Text>();
            title.text = "Вход / Регистрация";
            title.alignment = TextAnchor.MiddleCenter;
            title.color = Color.white;
            title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var trect = title.GetComponent<RectTransform>();
            trect.sizeDelta = new Vector2(360, 28);

            _emailInput = CreateInput(form.transform, "Email или Ник", Vector2.zero, false);
            _passwordInput = CreateInput(form.transform, "Пароль", Vector2.zero, true);

            var loginBtn = CreateButton(form.transform, "Войти", Vector2.zero, OnAccountLoginClicked);
            var regBtn = CreateButton(form.transform, "Регистрация", Vector2.zero, OnAccountRegisterClicked);
            var backBtn = CreateButton(form.transform, "Назад", Vector2.zero, OnBackClicked);

            var statusGo = new GameObject("Status");
            statusGo.transform.SetParent(form.transform, false);
            _statusText = statusGo.AddComponent<Text>();
            _statusText.text = "";
            _statusText.alignment = TextAnchor.MiddleCenter;
            _statusText.color = new Color(1f, 0.85f, 0.2f, 1f);
            _statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var srect = _statusText.GetComponent<RectTransform>();
            srect.sizeDelta = new Vector2(360, 24);

            // Правая колонка: картинка/арт
            var artCol = new GameObject("ArtColumn");
            artCol.transform.SetParent(split.transform, false);
            var artColRect = artCol.AddComponent<RectTransform>();
            var artColLE = artCol.AddComponent<LayoutElement>();
            artColLE.flexibleWidth = 1; // займёт остальное пространство
            artColLE.minWidth = 320;

            // фон под арт чуть темнее краёв
            var artBG = artCol.AddComponent<Image>();
            artBG.color = new Color(0.04f, 0.04f, 0.06f, 0.8f);

            BuildSideArt(artCol.transform);

            _accountPanel.SetActive(false);
        }

        private void BuildSideArt(Transform parent)
        {
            // Пытаемся загрузить Sprite из Resources/Auth/SideArt
            var sideSprite = Resources.Load<Sprite>("Auth/SideArt");
            if (sideSprite != null)
            {
                var imgGo = new GameObject("SideArtSprite");
                imgGo.transform.SetParent(parent, false);
                var imgRect = imgGo.AddComponent<RectTransform>();
                imgRect.anchorMin = new Vector2(0f, 0f);
                imgRect.anchorMax = new Vector2(1f, 1f);
                imgRect.offsetMin = new Vector2(12, 12);
                imgRect.offsetMax = new Vector2(-12, -12);
                var image = imgGo.AddComponent<Image>();
                image.sprite = sideSprite;
                image.preserveAspect = true;
                image.color = Color.white;
            }
            else
            {
                // Генерируем простую космическую подложку (градиент + звёзды)
                var rawGo = new GameObject("SideArtGenerated");
                rawGo.transform.SetParent(parent, false);
                var rawRect = rawGo.AddComponent<RectTransform>();
                rawRect.anchorMin = new Vector2(0f, 0f);
                rawRect.anchorMax = new Vector2(1f, 1f);
                rawRect.offsetMin = new Vector2(12, 12);
                rawRect.offsetMax = new Vector2(-12, -12);
                var raw = rawGo.AddComponent<RawImage>();
                raw.texture = GenerateSpaceTexture(768, 512);
                raw.uvRect = new Rect(0, 0, 1, 1);
            }
        }

        private Texture2D GenerateSpaceTexture(int width, int height)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];

            // Градиент фона: фиолетовый -> сине-бирюзовый
            Color top = new Color(0.06f, 0.02f, 0.10f, 1f);
            Color bottom = new Color(0.02f, 0.18f, 0.34f, 1f);
            for (int y = 0; y < height; y++)
            {
                float t = (float)y / (height - 1);
                var row = Color.Lerp(top, bottom, t);
                for (int x = 0; x < width; x++)
                {
                    pixels[y * width + x] = row;
                }
            }

            // Лёгкое шумовое облако
            var rnd = new System.Random();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (rnd.NextDouble() < 0.002)
                    {
                        float a = 0.08f + (float)rnd.NextDouble() * 0.12f;
                        pixels[y * width + x] += new Color(a * 0.4f, a * 0.6f, a, a);
                    }
                }
            }

            // Звёзды
            int stars = (width * height) / 1200;
            for (int i = 0; i < stars; i++)
            {
                int sx = rnd.Next(1, width - 1);
                int sy = rnd.Next(1, height - 1);
                pixels[sy * width + sx] = Color.white;
                // небольшое свечение
                var glow = new Color(1f, 1f, 1f, 0.5f);
                pixels[sy * width + (sx - 1)] = Color.Lerp(pixels[sy * width + (sx - 1)], glow, 0.5f);
                pixels[sy * width + (sx + 1)] = Color.Lerp(pixels[sy * width + (sx + 1)], glow, 0.5f);
                pixels[(sy - 1) * width + sx] = Color.Lerp(pixels[(sy - 1) * width + sx], glow, 0.5f);
                pixels[(sy + 1) * width + sx] = Color.Lerp(pixels[(sy + 1) * width + sx], glow, 0.5f);
            }

            tex.SetPixels(pixels);
            tex.Apply(false);
            return tex;
        }

        private Button CreateButton(Transform parent, string text, Vector2 pos, UnityEngine.Events.UnityAction onClick)
        {
            var btnGo = new GameObject($"Button_{text}");
            btnGo.transform.SetParent(parent, false);
            var rect = btnGo.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(220, 48);
            rect.anchoredPosition = pos;

            var img = btnGo.AddComponent<Image>();
            img.color = new Color(0.2f, 0.4f, 0.8f, 1f);

            // Чтобы элемент не схлопывался внутри VerticalLayoutGroup
            var le = btnGo.AddComponent<LayoutElement>();
            le.preferredHeight = 48;
            le.minHeight = 40;
            le.preferredWidth = 320;
            le.flexibleWidth = 1;

            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            var labelGo = new GameObject("Text");
            labelGo.transform.SetParent(btnGo.transform, false);
            var txt = labelGo.AddComponent<Text>();
            txt.text = text;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var lrect = txt.GetComponent<RectTransform>();
            lrect.anchorMin = Vector2.zero;
            lrect.anchorMax = Vector2.one;
            lrect.offsetMin = Vector2.zero;
            lrect.offsetMax = Vector2.zero;

            return btn;
        }

        private InputField CreateInput(Transform parent, string placeholder, Vector2 pos, bool isPassword)
        {
            var go = new GameObject($"Input_{placeholder}");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(280, 36);
            rect.anchoredPosition = pos;

            var img = go.AddComponent<Image>();
            img.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            // Чтобы элемент не схлопывался внутри VerticalLayoutGroup
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 36;
            le.minHeight = 32;
            le.preferredWidth = 320;
            le.flexibleWidth = 1;

            var input = go.AddComponent<InputField>();
            input.textComponent = CreateLabel(go.transform, "", TextAnchor.MiddleLeft, new Vector2(10, 0));
            input.placeholder = CreateLabel(go.transform, placeholder, TextAnchor.MiddleLeft, new Vector2(10, 0), 0.5f);
            input.contentType = isPassword ? InputField.ContentType.Password : InputField.ContentType.Standard;
            input.lineType = InputField.LineType.SingleLine;
            return input;
        }

        private Text CreateLabel(Transform parent, string text, TextAnchor anchor, Vector2 padding, float alpha = 1f)
        {
            var labelGo = new GameObject("Text");
            labelGo.transform.SetParent(parent, false);
            var txt = labelGo.AddComponent<Text>();
            txt.text = text;
            txt.alignment = anchor;
            txt.color = new Color(1f, 1f, 1f, alpha);
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var r = txt.GetComponent<RectTransform>();
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(8 + padding.x, 6 + padding.y);
            r.offsetMax = new Vector2(-8, -6);
            return txt;
        }

        
        private static string DerivePreferredName(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            var s = input.Trim();
            var at = s.IndexOf('@');
            if (at > 0) s = s.Substring(0, at);
            s = System.Text.RegularExpressions.Regex.Replace(s, "[^A-Za-z0-9._-]", "");
            if (s.Length > 24) s = s.Substring(0, 24);
            if (s.Length < 3) return null;
            return s;
        }private void OnPlayClicked()
        {
            if (string.IsNullOrEmpty(gameSceneName))
            {
                Debug.LogError("AuthBootstrap: Game scene name is empty.");
                return;
            }
            SceneManager.LoadScene(gameSceneName);
        }

        private void OnLoginClicked()
        {
            _mainPanel.SetActive(false);
            _accountPanel.SetActive(true);
        }

        private void OnBackClicked()
        {
            _accountPanel.SetActive(false);
            _mainPanel.SetActive(true);
        }

        private void OnAccountLoginClicked()
        {
            var email = _emailInput?.text?.Trim() ?? string.Empty;
            var pass = _passwordInput?.text ?? string.Empty;
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
            {
                if (_statusText) _statusText.text = "Введите email/ник и пароль";
                return;
            }
            if (UgsAuthService.Instance != null)
            {
                if (_statusText) _statusText.text = "Входим...";
                var pref = DerivePreferredName(email); if (!string.IsNullOrEmpty(pref)) { PlayerPrefs.SetString("profile.player_name", pref); PlayerPrefs.Save(); } _ = UgsAuthService.Instance.SignInAsync(email, pass);
            }
        }

        private void OnAccountRegisterClicked()
        {
            var email = _emailInput?.text?.Trim() ?? string.Empty;
            var pass = _passwordInput?.text ?? string.Empty;
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
            {
                if (_statusText) _statusText.text = "Введите email/ник и пароль";
                return;
            }
            if (UgsAuthService.Instance != null)
            {
                if (_statusText) _statusText.text = "Регистрируем...";
                var pref = DerivePreferredName(email); if (!string.IsNullOrEmpty(pref)) { PlayerPrefs.SetString("profile.player_name", pref); PlayerPrefs.Save(); } _ = UgsAuthService.Instance.RegisterAsync(email, pass);
            }
        }

        private void OnUgsSignedIn()
        {
            if (_statusText) _statusText.text = "Успешный вход";
            if (!string.IsNullOrEmpty(gameSceneName))
            {
                SceneManager.LoadScene(gameSceneName);
            }
        }

        private void OnUgsError(string msg)
        {
            if (_statusText) _statusText.text = msg;
        }
    }
}


