using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace CityDino
{
    /// <summary>Scene entry, input, persistence and menus. The simulation owns all gameplay state.</summary>
    [DisallowMultipleComponent]
    public sealed class CityRunnerController : MonoBehaviour
    {
        private const string SaveKey = "CityDino.Save.v1";
        private const float CanvasWidth = 1280f;
        private const float CanvasHeight = 720f;
        private static readonly Rect MoveLeftButton = new Rect(38, 618, 88, 70);
        private static readonly Rect MoveRightButton = new Rect(136, 618, 88, 70);
        private static readonly Rect DuckButton = new Rect(1000, 618, 112, 70);
        private static readonly Rect JumpButton = new Rect(1126, 618, 112, 70);
        private RunnerSession session;
        private RunnerArt art;
        private RunnerWorldView world;
        private GUIStyle label, button, panel;
        private Texture2D roundTexture;
        private AudioSource audioSource;
        private AudioClip jumpSound, coinSound, hurtSound, buySound;
        private bool muted, showShop, showHistory, dirty;
        private float nextSave, ambientTime, collectedFlash;
        private int bestAtStart;
        private string notice;
        private float noticeUntil;
        private readonly Color ink = new Color32(24, 43, 51, 255);
        private readonly Color cream = new Color32(249, 246, 233, 255);
        private readonly Color mint = new Color32(190, 231, 164, 255);
        private readonly Color orange = new Color32(255, 155, 82, 255);
        private readonly Color soft = new Color32(103, 119, 118, 255);

        // Opening the supplied SampleScene also starts the new game, without editing the old scene.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureEntryPoint()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.name != "CityRunner" && scene.name != "SampleScene") return;
            if (FindFirstObjectByType<CityRunnerController>() != null) return;
            foreach (GameObject root in scene.GetRootGameObjects()) root.SetActive(false);
            var cameraObject = new GameObject("City Runner Camera", typeof(Camera), typeof(AudioListener));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(24, 43, 51, 255);
            camera.orthographic = true;
            new GameObject("City Runner", typeof(CityRunnerController));
        }

        private void Awake()
        {
            Time.timeScale = 1f;
            Application.targetFrameRate = 60;
            if (Application.isMobilePlatform) Screen.orientation = ScreenOrientation.LandscapeLeft;
            art = new RunnerArt();
            world = new RunnerWorldView(art);
            RunnerSave save = null;
            if (PlayerPrefs.HasKey(SaveKey))
            {
                try { save = JsonUtility.FromJson<RunnerSave>(PlayerPrefs.GetString(SaveKey)); }
                catch (Exception) { Debug.LogWarning("City Dino: không đọc được dữ liệu lưu; tạo hồ sơ mới."); }
            }
            if (save == null) save = new RunnerSave();
            save.BestScore = Math.Max(save.BestScore, PlayerPrefs.GetInt("HighScore", 0));
            session = new RunnerSession(save, Environment.TickCount);
            session.SaveChanged += SaveProgress;
            muted = PlayerPrefs.GetInt("CityDino.Muted", 0) == 1;
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.volume = 0.22f;
            jumpSound = MakeTone("Jump", 390, 650, 0.10f);
            coinSound = MakeTone("Star", 980, 1420, 0.10f);
            hurtSound = MakeTone("Hit", 180, 65, 0.20f);
            buySound = MakeTone("Upgrade", 550, 1120, 0.24f);
        }

        private void Update()
        {
            if (session == null) return;
            ambientTime += Time.unscaledDeltaTime;
            Keyboard keys = Keyboard.current;
            bool jump = keys != null && (keys.spaceKey.wasPressedThisFrame || keys.upArrowKey.wasPressedThisFrame || keys.wKey.wasPressedThisFrame);
            bool duck = keys != null && (keys.downArrowKey.isPressed || keys.sKey.isPressed);
            float horizontal = 0;
            if (keys != null)
            {
                if (keys.leftArrowKey.isPressed || keys.aKey.isPressed) horizontal -= 1;
                if (keys.rightArrowKey.isPressed || keys.dKey.isPressed) horizontal += 1;
            }
            if (keys != null && keys.escapeKey.wasPressedThisFrame)
            {
                if (showShop || showHistory) { showShop = false; showHistory = false; }
                else session.TogglePause();
            }
            if (keys != null && keys.mKey.wasPressedThisFrame) ToggleSound();
            if (session.State == RunState.Menu && !showShop && !showHistory && jump) { BeginRun(); jump = false; }
            if (session.State == RunState.GameOver && keys != null && keys.rKey.wasPressedThisFrame) BeginRun();

            if (session.State == RunState.Playing)
            {
                ReadPointers(ref horizontal, ref jump, ref duck);
                session.SetHorizontal(horizontal);
                session.SetDucking(duck);
                if (jump && session.Jump()) Play(jumpSound);
            }
            int oldStars = session.RunStars;
            int oldLives = session.Lives;
            RunState oldState = session.State;
            // Real-time spikes are discarded; the core subdivides normal simulation time for collision safety.
            session.Update(Mathf.Min(Time.unscaledDeltaTime, 0.1f));
            if (session.RunStars > oldStars) { Play(coinSound); collectedFlash = 0.65f; }
            if (session.Lives < oldLives) Play(hurtSound);
            if (session.Lives > oldLives && oldState == RunState.Playing) Play(buySound);
            if (oldState != RunState.GameOver && session.State == RunState.GameOver) FlushSave();
            collectedFlash = Mathf.Max(0, collectedFlash - Time.unscaledDeltaTime);
            if (dirty && Time.unscaledTime >= nextSave) FlushSave();
        }

        private static void ReadPointers(ref float horizontal, ref bool jump, ref bool duck)
        {
            bool activeTouch = false;
            if (Touchscreen.current != null)
            {
                var touches = Touchscreen.current.touches;
                for (int i = 0; i < touches.Count; i++)
                {
                    var touch = touches[i];
                    bool down = touch.press.wasPressedThisFrame;
                    bool held = touch.press.isPressed;
                    if (!down && !held) continue;
                    activeTouch = true;
                    ApplyPointer(touch.position.ReadValue(), down, held, ref horizontal, ref jump, ref duck);
                }
            }
            if (!activeTouch && Mouse.current != null)
            {
                ApplyPointer(Mouse.current.position.ReadValue(), Mouse.current.leftButton.wasPressedThisFrame,
                    Mouse.current.leftButton.isPressed, ref horizontal, ref jump, ref duck);
            }
        }

        private static void ApplyPointer(Vector2 raw, bool down, bool held, ref float horizontal, ref bool jump, ref bool duck)
        {
            float scale = Mathf.Min(Screen.width / CanvasWidth, Screen.height / CanvasHeight);
            Vector2 offset = new Vector2((Screen.width - CanvasWidth * scale) * 0.5f, (Screen.height - CanvasHeight * scale) * 0.5f);
            Vector2 point = (new Vector2(raw.x, Screen.height - raw.y) - offset) / Mathf.Max(scale, 0.001f);
            if (point.x < 0 || point.x > CanvasWidth || point.y <= 130 || point.y >= 710) return;
            if (MoveLeftButton.Contains(point)) { if (held) horizontal -= 1; }
            else if (MoveRightButton.Contains(point)) { if (held) horizontal += 1; }
            else if (DuckButton.Contains(point)) { if (held) duck = true; }
            else if (down) jump = true;
        }

        private void BeginRun()
        {
            showShop = showHistory = false;
            bestAtStart = session.Save.BestScore;
            session.StartRun();
        }

        private void OnGUI()
        {
            if (session == null || world == null) return;
            EnsureStyles();
            GUI.color = ink;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            Matrix4x4 previous = GUI.matrix;
            float scale = Mathf.Min(Screen.width / CanvasWidth, Screen.height / CanvasHeight);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - CanvasWidth * scale) * 0.5f, (Screen.height - CanvasHeight * scale) * 0.5f, 0), Quaternion.identity, Vector3.one * scale);
            world.Draw(session, ambientTime);
            switch (session.State)
            {
                case RunState.Menu:
                    GUI.enabled = !(showShop || showHistory);
                    DrawMenu();
                    GUI.enabled = true;
                    break;
                case RunState.Playing: DrawHud(); DrawPlayHints(); break;
                case RunState.Paused:
                    GUI.enabled = false;
                    DrawHud();
                    GUI.enabled = true;
                    DrawPaused();
                    break;
                case RunState.GameOver:
                    GUI.enabled = false;
                    DrawHud();
                    GUI.enabled = true;
                    DrawResults();
                    break;
            }
            if (showShop) DrawShop();
            if (showHistory) DrawHistory();
            if (Time.unscaledTime < noticeUntil) DrawNotice(notice);
            else if (session.ToastRemaining > 0 && !string.IsNullOrEmpty(session.Toast) && session.State == RunState.Playing) DrawNotice(session.Toast);
            GUI.matrix = previous;
            GUI.color = Color.white;
        }

        private void DrawMenu()
        {
            Box(new Rect(64, 56, 486, 602), cream);
            Text(new Rect(98, 85, 390, 24), "●  DINO CITY  /  ENDLESS RUNNER", 15, soft, FontStyle.Bold);
            Text(new Rect(94, 128, 420, 148), "CHẠY QUA\nTHÀNH PHỐ.", 58, ink, FontStyle.Bold);
            Text(new Rect(98, 292, 392, 56), "Đổi vị trí, vượt bẫy và làm chủ nhịp chạy.\nThành phố mới, tốc độ mới, thử thách mới.", 18, soft);
            Stat(new Rect(98, 366, 124, 64), "KỶ LỤC", session.Save.BestScore.ToString("N0"));
            Stat(new Rect(238, 366, 124, 64), "LƯỢT TRƯỚC", session.Save.LastScore.ToString("N0"));
            Stat(new Rect(378, 366, 124, 64), "VÍ SAO", session.Save.Wallet.ToString("N0"));
            if (Button(new Rect(98, 458, 418, 62), "BẮT ĐẦU CHẠY     →", mint, ink, 22)) BeginRun();
            if (Button(new Rect(98, 534, 202, 52), "Cửa hàng", ink, cream)) showShop = true;
            if (Button(new Rect(314, 534, 202, 52), "Thành tích", new Color32(231, 231, 215, 255), ink)) showHistory = true;
            Text(new Rect(98, 606, 420, 24), "BẢN 02  ·  SPACE để bắt đầu  ·  Có cảm ứng", 14, soft);

            Box(new Rect(782, 78, 408, 47), new Color(1, 1, 1, 0.82f));
            Text(new Rect(800, 90, 372, 24), "BÌNH MINH MỚI. KỶ LỤC MỚI.", 17, ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            if (art.DinoFrames != null && art.DinoFrames.Length > 0)
            {
                int frame = Mathf.FloorToInt(ambientTime * 7) % art.DinoFrames.Length;
                if (art.DinoFrames[frame] != null)
                    GUI.DrawTexture(new Rect(794, 246 + Mathf.Sin(ambientTime * 2) * 5, 228, 247), art.DinoFrames[frame], ScaleMode.ScaleToFit);
            }
            Box(new Rect(806, 586, 376, 62), ink);
            Icon(art.Heart, new Rect(824, 602, 28, 26));
            Text(new Rect(866, 598, 304, 36), "Khởi đầu với " + (1 + session.Save.ExtraHeartsLevel) + " mạng mỗi lượt", 17, cream, FontStyle.Bold, TextAnchor.MiddleLeft);
            if (Button(new Rect(1152, 654, 72, 36), muted ? "Âm: tắt" : "Âm: bật", cream, ink, 13)) ToggleSound();
        }

        private void DrawHud()
        {
            Box(new Rect(36, 26, 1208, 86), cream);
            Text(new Rect(58, 43, 170, 19), "DINO CITY", 16, ink, FontStyle.Bold);
            Text(new Rect(58, 70, 180, 23), DistrictName(session.CurrentDistrict), 14, soft);
            Text(new Rect(246, 40, 180, 21), "ĐIỂM", 12, soft, FontStyle.Bold);
            Text(new Rect(244, 60, 182, 39), session.Score.ToString("D5"), 30, ink, FontStyle.Bold);
            Text(new Rect(452, 40, 155, 21), "KỶ LỤC", 12, soft, FontStyle.Bold);
            Text(new Rect(450, 65, 168, 29), Math.Max(session.Score, session.Save.BestScore).ToString("D5"), 22, ink, FontStyle.Bold);
            for (int i = 0; i < 5; i++)
            {
                Color previous = GUI.color;
                GUI.color = i < session.Lives ? Color.white : new Color(0.55f, 0.6f, 0.6f, 0.24f);
                Icon(art.Heart, new Rect(660 + i * 30, 55, 26, 26));
                GUI.color = previous;
            }
            Icon(art.Star, new Rect(852, 48, 32, 32));
            Text(new Rect(897, 43, 124, 26), "+" + session.RunStars, 24, ink, FontStyle.Bold);
            Text(new Rect(897, 75, 150, 20), "Ví: " + session.Save.Wallet, 13, soft);
            if (Button(new Rect(1070, 43, 54, 50), muted ? "♫ ×" : "♫", new Color32(231, 231, 215, 255), ink, 18)) ToggleSound();
            if (Button(new Rect(1136, 43, 82, 50), "II", ink, cream, 22)) session.TogglePause();
        }

        private void DrawPlayHints()
        {
            Box(MoveLeftButton, cream);
            Text(MoveLeftButton, "←", 30, ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            Box(MoveRightButton, cream);
            Text(MoveRightButton, "→", 30, ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            Box(new Rect(242, 632, 728, 56), new Color(0.06f, 0.13f, 0.18f, 0.88f));
            Text(new Rect(260, 640, 696, 41), "← → / A D: di chuyển   ·   SPACE / ↑: nhảy kép\n↓ / S: cúi tránh bẫy   ·   ESC: tạm dừng", 15, cream);
            Box(DuckButton, cream);
            Text(DuckButton, "CÚI ↓", 19, ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            Box(JumpButton, mint);
            Text(JumpButton, "NHẢY ↑", 19, ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            Text(new Rect(38, 592, 430, 28), Mathf.FloorToInt(session.Distance / 20f) + " m  ·  " + (session.Speed / RunnerSession.InitialSpeed).ToString("0.00") + "×  ·  Chặng " + session.StageNumber, 15, cream, FontStyle.Bold);
            if (collectedFlash > 0)
                Text(new Rect(session.PlayerX - 20, 398 - (0.65f - collectedFlash) * 50 - session.PlayerY, 90, 40), "+ SAO", 20, orange, FontStyle.Bold);
        }

        private void DrawPaused()
        {
            Scrim();
            Box(new Rect(410, 182, 460, 370), cream);
            Text(new Rect(442, 213, 396, 26), "NGHỈ CHÂN MỘT CHÚT", 15, soft, FontStyle.Bold, TextAnchor.MiddleCenter);
            Text(new Rect(442, 252, 396, 56), "Tạm dừng", 40, ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            Text(new Rect(448, 321, 384, 30), "Chuyến đi của bạn vẫn đang chờ.", 18, soft, FontStyle.Normal, TextAnchor.MiddleCenter);
            if (Button(new Rect(448, 381, 384, 59), "TIẾP TỤC    →", mint, ink, 21)) session.TogglePause();
            if (Button(new Rect(448, 455, 384, 48), "Kết thúc lượt và về menu", new Color32(231, 231, 215, 255), ink, 17)) GoToMenu();
        }

        private void DrawResults()
        {
            Scrim();
            Box(new Rect(372, 128, 536, 520), cream);
            bool newBest = session.Score > bestAtStart;
            Text(new Rect(412, 157, 456, 28), newBest ? "KỶ LỤC MỚI!" : "MỖI CHUYẾN ĐI LÀ MỘT THÀNH TÍCH", 15, newBest ? new Color32(182, 97, 26, 255) : soft, FontStyle.Bold, TextAnchor.MiddleCenter);
            Text(new Rect(412, 205, 456, 58), "Hẹn ở lượt tiếp!", 40, ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            Text(new Rect(412, 272, 456, 30), EndMessage(), 17, soft, FontStyle.Normal, TextAnchor.MiddleCenter);
            Stat(new Rect(416, 328, 142, 73), "ĐIỂM LƯỢT NÀY", session.Score.ToString("N0"));
            Stat(new Rect(580, 328, 142, 73), "KỶ LỤC", session.Save.BestScore.ToString("N0"));
            Icon(art.Star, new Rect(751, 338, 25, 25));
            Text(new Rect(786, 332, 77, 36), "+" + session.RunStars, 29, ink, FontStyle.Bold);
            Text(new Rect(748, 377, 120, 20), "Đã cộng vào ví", 13, soft);
            Text(new Rect(416, 421, 448, 27), "Quãng đường: " + Mathf.FloorToInt(session.Distance / 20f) + " m   ·   Chặng " + session.StageNumber, 16, soft, FontStyle.Normal, TextAnchor.MiddleCenter);
            if (Button(new Rect(414, 475, 452, 60), "CHẠY LẠI    →", mint, ink, 21)) BeginRun();
            if (Button(new Rect(414, 550, 452, 50), "Về menu & nâng cấp", ink, cream, 18)) GoToMenu();
            Text(new Rect(414, 613, 452, 22), "Nhấn R để chơi lại", 13, soft, FontStyle.Normal, TextAnchor.MiddleCenter);
        }

        private string EndMessage()
        {
            if (session.EndReason != null && session.EndReason.IndexOf("Rơi", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Lỡ một nhịp nhảy. Thử lại nhé!";
            if (session.EndReason != null && session.EndReason.IndexOf("máy bay", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Máy bay bay thấp: giữ ↓ hoặc nút CÚI để tránh.";
            return "Hết mạng rồi. Gom thêm sao để khởi đầu tốt hơn!";
        }

        private void DrawShop()
        {
            Scrim();
            Box(new Rect(342, 112, 596, 536), cream);
            Text(new Rect(380, 139, 440, 25), "TRẠM NÂNG CẤP", 15, soft, FontStyle.Bold);
            if (Button(new Rect(858, 134, 45, 38), "×", new Color32(231, 231, 215, 255), ink, 23)) showShop = false;
            Text(new Rect(378, 183, 500, 53), "Đi xa hơn một chút.", 35, ink, FontStyle.Bold);
            Icon(art.Star, new Rect(380, 253, 32, 32));
            Text(new Rect(425, 251, 420, 34), session.Save.Wallet + " sao trong ví", 23, ink, FontStyle.Bold);
            Box(new Rect(378, 309, 524, 146), new Color32(232, 237, 217, 255));
            Icon(art.Heart, new Rect(398, 335, 64, 62));
            Text(new Rect(483, 328, 390, 32), "Thêm mạng khởi đầu", 23, ink, FontStyle.Bold);
            Text(new Rect(483, 370, 380, 53), "Nâng cấp vĩnh viễn · Cấp " + session.Save.ExtraHeartsLevel + "/3\nMỗi lượt bắt đầu với " + (1 + session.Save.ExtraHeartsLevel) + " mạng.", 17, soft);
            int price = session.NextHeartCost;
            bool canBuy = price > 0 && session.Save.Wallet >= price;
            string text = price <= 0 ? "ĐÃ NÂNG CẤP TỐI ĐA" : "THÊM 1 MẠNG    ·    " + price + " SAO";
            if (Button(new Rect(378, 481, 524, 61), text, canBuy ? mint : new Color32(221, 223, 211, 255), ink, 21, canBuy))
            {
                if (session.BuyExtraHeart()) { Play(buySound); Notify("Đã nâng cấp! Mạng mới có từ lượt chơi tiếp theo."); FlushSave(); }
            }
            Text(new Rect(382, 562, 516, 51), price <= 0 ? "Bạn đã có 4 mạng khởi đầu. Tim nhặt trong màn hồi tối đa 5 mạng." : (canBuy ? "Sao đã nhặt luôn được giữ lại, kể cả khi hết mạng." : "Cần thêm " + Math.Max(0, price - session.Save.Wallet) + " sao. Chơi thêm một lượt nhé!"), 16, soft, FontStyle.Normal, TextAnchor.MiddleCenter);
        }

        private void DrawHistory()
        {
            Scrim();
            Box(new Rect(310, 90, 660, 558), cream);
            Text(new Rect(348, 120, 520, 27), "NHẬT KÝ NHỮNG CHUYẾN ĐI", 15, soft, FontStyle.Bold);
            if (Button(new Rect(890, 112, 45, 38), "×", new Color32(231, 231, 215, 255), ink, 23)) showHistory = false;
            Text(new Rect(346, 164, 540, 52), "Hôm nay bạn đã đi bao xa?", 31, ink, FontStyle.Bold);
            Text(new Rect(350, 237, 260, 24), "Kỷ lục: " + session.Save.BestScore.ToString("N0") + " điểm", 21, ink, FontStyle.Bold);
            if (session.Save.History == null || session.Save.History.Count == 0)
                Text(new Rect(350, 331, 580, 98), "Chưa có lượt chơi nào.\nBắt đầu chuyến đi đầu tiên của bạn!", 21, soft, FontStyle.Normal, TextAnchor.MiddleCenter);
            else
            {
                Text(new Rect(350, 294, 580, 24), "LƯỢT                        ĐIỂM                 MÉT             SAO", 13, soft, FontStyle.Bold);
                int count = Math.Min(session.Save.History.Count, 5);
                for (int i = 0; i < count; i++)
                {
                    // History is stored newest first by the simulation.
                    RunRecord record = session.Save.History[i];
                    float y = 331 + i * 47;
                    Box(new Rect(346, y, 588, 41), i % 2 == 0 ? new Color32(234, 236, 221, 255) : cream);
                    Text(new Rect(361, y + 9, 130, 24), i == 0 ? "Vừa chơi" : "Trước đó " + i, 16, soft);
                    Text(new Rect(528, y + 6, 130, 28), record.Score.ToString("N0"), 20, ink, FontStyle.Bold);
                    Text(new Rect(700, y + 9, 110, 24), Mathf.FloorToInt(record.Distance / 20f).ToString(), 16, soft);
                    Text(new Rect(856, y + 9, 75, 24), "+" + record.Stars, 16, ink, FontStyle.Bold);
                }
            }
            Text(new Rect(350, 598, 580, 23), "Điểm, sao và nâng cấp được lưu tự động trên thiết bị này.", 15, soft, FontStyle.Normal, TextAnchor.MiddleCenter);
        }

        private void GoToMenu()
        {
            session.ReturnToMenu();
            showShop = showHistory = false;
            FlushSave();
        }

        private void SaveProgress()
        {
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(session.Save));
            dirty = true;
            nextSave = Time.unscaledTime + 1f;
        }
        private void FlushSave() { if (dirty) { PlayerPrefs.Save(); dirty = false; } }
        private void OnApplicationFocus(bool focused)
        {
            if (focused || session == null) return;
            if (session.State == RunState.Playing) session.TogglePause();
            FlushSave();
        }
        private void OnApplicationPause(bool paused) { if (paused) OnApplicationFocus(false); }
        private void OnApplicationQuit()
        {
            if (session != null && (session.State == RunState.Playing || session.State == RunState.Paused)) session.ReturnToMenu();
            FlushSave();
        }
        private void OnDestroy()
        {
            if (session != null) session.SaveChanged -= SaveProgress;
            FlushSave();
            if (world != null) world.Dispose();
            if (roundTexture != null) Destroy(roundTexture);
            if (jumpSound != null) Destroy(jumpSound);
            if (coinSound != null) Destroy(coinSound);
            if (hurtSound != null) Destroy(hurtSound);
            if (buySound != null) Destroy(buySound);
        }

        private void EnsureStyles()
        {
            if (label != null) return;
            roundTexture = new Texture2D(24, 24, TextureFormat.RGBA32, false);
            roundTexture.name = "CityDino UI corners";
            roundTexture.hideFlags = HideFlags.HideAndDontSave;
            roundTexture.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < 24; y++)
                for (int x = 0; x < 24; x++)
                {
                    float dx = Mathf.Max(0, Mathf.Abs(x - 11.5f) - 4f);
                    float dy = Mathf.Max(0, Mathf.Abs(y - 11.5f) - 4f);
                    roundTexture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(8f - Mathf.Sqrt(dx * dx + dy * dy))));
                }
            roundTexture.Apply();
            label = new GUIStyle(GUI.skin.label) { font = art.Font, padding = new RectOffset(0, 0, 0, 0), wordWrap = false, richText = false };
            button = new GUIStyle(GUI.skin.button)
            {
                font = art.Font,
                border = new RectOffset(10, 10, 10, 10),
                padding = new RectOffset(12, 12, 5, 5),
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            button.normal.background = roundTexture;
            button.hover.background = roundTexture;
            button.active.background = roundTexture;
            button.focused.background = roundTexture;
            panel = new GUIStyle { border = new RectOffset(10, 10, 10, 10) };
            panel.normal.background = roundTexture;
        }

        private void Box(Rect rect, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.Box(rect, GUIContent.none, panel);
            GUI.color = old;
        }
        private void Text(Rect rect, string text, int size, Color color, FontStyle style = FontStyle.Normal, TextAnchor align = TextAnchor.UpperLeft)
        {
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = align;
            label.normal.textColor = color;
            GUI.Label(rect, text, label);
        }
        private bool Button(Rect rect, string text, Color background, Color foreground, int size = 19, bool enabled = true)
        {
            bool wasEnabled = GUI.enabled;
            GUI.enabled = enabled && wasEnabled;
            Color old = GUI.backgroundColor;
            GUI.backgroundColor = background;
            button.fontSize = size;
            button.normal.textColor = foreground;
            button.hover.textColor = foreground;
            button.active.textColor = foreground;
            button.focused.textColor = foreground;
            bool pressed = GUI.Button(rect, text, button);
            GUI.backgroundColor = old;
            GUI.enabled = wasEnabled;
            return pressed;
        }
        private void Stat(Rect rect, string heading, string value)
        {
            Text(new Rect(rect.x, rect.y, rect.width, 22), heading, 12, soft, FontStyle.Bold);
            Text(new Rect(rect.x - 1, rect.y + 29, rect.width, 37), value, 28, ink, FontStyle.Bold);
        }
        private static void Icon(Texture2D texture, Rect rect)
        {
            if (texture != null) GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit);
        }
        private void Scrim()
        {
            Color old = GUI.color;
            GUI.color = new Color(0.03f, 0.09f, 0.14f, 0.74f);
            GUI.DrawTexture(new Rect(0, 0, CanvasWidth, CanvasHeight), Texture2D.whiteTexture);
            GUI.color = old;
        }
        private void Notify(string text) { notice = text; noticeUntil = Time.unscaledTime + 3f; }
        private void DrawNotice(string text)
        {
            Box(new Rect(330, 127, 620, 49), ink);
            Text(new Rect(344, 135, 592, 30), text, 16, cream, FontStyle.Bold, TextAnchor.MiddleCenter);
        }
        private void ToggleSound()
        {
            muted = !muted;
            PlayerPrefs.SetInt("CityDino.Muted", muted ? 1 : 0);
            PlayerPrefs.Save();
        }
        private void Play(AudioClip clip) { if (!muted && audioSource != null && clip != null) audioSource.PlayOneShot(clip); }
        private static AudioClip MakeTone(string name, float from, float to, float seconds)
        {
            const int rate = 22050;
            var samples = new float[Mathf.CeilToInt(seconds * rate)];
            double phase = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / samples.Length;
                float envelope = Mathf.Min(t * 15, 1) * (1 - t) * (1 - t);
                phase += 2 * Math.PI * Mathf.Lerp(from, to, t) / rate;
                samples[i] = (float)Math.Sin(phase) * envelope * 0.38f;
            }
            AudioClip clip = AudioClip.Create("CityDino " + name, samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }
        public static string DistrictName(District district)
        {
            switch (district)
            {
                case District.Rain: return "Phố mưa";
                case District.Sunset: return "Nắng chiều";
                case District.Night: return "Phố đêm";
                case District.Skyway: return "Thành phố trên mây";
                case District.Storm: return "Vùng giông";
                default: return "Bình minh";
            }
        }
    }
}
