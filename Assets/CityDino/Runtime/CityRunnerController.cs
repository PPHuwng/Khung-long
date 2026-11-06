using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace CityDino
{
    /// <summary>Scene entry, input, persistence and menus. The simulation owns all gameplay state.</summary>
    [DisallowMultipleComponent]
    public sealed partial class CityRunnerController : MonoBehaviour
    {
        private const string SaveKey = "CityDino.Save.v1";
        private const string StartingDistrictKey = "CityDino.StartDistrict";
        private const float CanvasWidth = 1280f;
        private const float CanvasHeight = 720f;
        private const float SaveCheckpointInterval = 20f;
        private static readonly Rect MoveLeftButton = new Rect(38, 618, 88, 70);
        private static readonly Rect MoveRightButton = new Rect(136, 618, 88, 70);
        private static readonly Rect DuckButton = new Rect(1000, 618, 112, 70);
        private static readonly Rect JumpButton = new Rect(1126, 618, 112, 70);
        private static readonly Rect ShootButton = new Rect(724, 618, 120, 70);
        private static readonly Rect SkillButton = new Rect(854, 618, 120, 70);
        private RunnerSession session;
        private RunnerArt art;
        private RunnerWorldView world;
        private GUIStyle label, button, panel;
        private Texture2D roundTexture;
        private AudioSource audioSource;
        private AudioClip jumpSound, coinSound, hurtSound, buySound, thunderSound, shootSound, skillSound;
        private bool muted, showShop, showHistory, showCharacters, showPractice, dirty;
        private BossKind practiceBoss = BossKind.Tank;
        private CharacterKind practiceCharacter = CharacterKind.Classic;
        private bool practiceLearning = true, practiceSettingsInitialized;
        private int practiceTier = 3;
        private float nextSave, ambientTime, nextShotSound;
        private int bestAtStart;
        private int shopTab;
        private int thunderStage, lastThunderStrike;
        private int hudScore = -1, hudBest = -1, hudStars = -1, hudWallet = -1;
        private string hudScoreText, hudBestText, hudStarsText, hudWalletText;
        private int hintMetres = -1, hintSpeedHundredths = -1, hintBossScore = -1;
        private string runningHint;
        private readonly Color ink = new Color32(24, 43, 51, 255);
        private readonly Color cream = new Color32(249, 246, 233, 255);
        private readonly Color mint = new Color32(190, 231, 164, 255);
        private readonly Color soft = new Color32(103, 119, 118, 255);
        private bool ModalOpen { get { return showShop || showHistory || showCharacters || showPractice; } }

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
            bool migrateSave = save.Version < 4;
            save.BestScore = Math.Max(save.BestScore, PlayerPrefs.GetInt("HighScore", 0));
            session = new RunnerSession(save, Environment.TickCount);
            session.SaveChanged += SaveProgress;
            if (migrateSave) { SaveProgress(); FlushSave(); }
            int startingDistrict = PlayerPrefs.GetInt(StartingDistrictKey, -1);
            session.SetStartingDistrict(Enum.IsDefined(typeof(District), startingDistrict)
                ? (District?)startingDistrict : null);
            muted = PlayerPrefs.GetInt("CityDino.Muted", 0) == 1;
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.volume = 0.22f;
            jumpSound = MakeTone("Jump", 390, 650, 0.10f);
            coinSound = MakeTone("Star", 980, 1420, 0.10f);
            hurtSound = MakeTone("Hit", 180, 65, 0.20f);
            buySound = MakeTone("Upgrade", 550, 1120, 0.24f);
            thunderSound = MakeThunder();
            shootSound = MakeTone("Shoot", 820, 360, 0.055f);
            skillSound = MakeTone("Skill", 330, 1050, 0.18f);
            InitializeBossAudio();
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
                if (ModalOpen) { showShop = showHistory = showCharacters = showPractice = false; }
                else session.TogglePause();
            }
            if (keys != null && keys.mKey.wasPressedThisFrame) ToggleSound();
            if (session.State == RunState.Menu && !ModalOpen && jump) { BeginRun(); jump = false; }
            if (session.State == RunState.GameOver && !ModalOpen && keys != null && keys.rKey.wasPressedThisFrame)
            {
                if (session.IsPractice) RetryPractice();
                else BeginRun();
            }

            bool manual = session.State == RunState.Playing && !ModalOpen &&
                (!session.IsBossArena || session.ArenaPhase == BossPhase.Fighting);
            if (manual)
            {
                bool canShoot = session.IsBossArena && session.ArenaPhase == BossPhase.Fighting;
                bool canSkill = session.CurrentCharacter != CharacterKind.Classic;
                bool flying = session.FlightControlsEnabled;
                bool swimming = session.SwimControlsEnabled;
                bool freeMovement = flying || swimming;
                bool shoot = canShoot && keys != null && keys.jKey.isPressed;
                bool skill = canSkill && keys != null && keys.eKey.wasPressedThisFrame;
                bool lift = freeMovement && keys != null && (keys.spaceKey.isPressed || keys.upArrowKey.isPressed || keys.wKey.isPressed);
                ReadMovementPointers(session.IsBossArena, canSkill, freeMovement, swimming,
                    ref horizontal, ref jump, ref duck, ref shoot, ref skill, ref lift);
                session.SetHorizontal(horizontal);
                session.SetDucking(!freeMovement && duck);
                session.SetShooting(canShoot && shoot);
                session.SetFlightLift(flying && lift);
                session.SetSwimLift(swimming && lift);
                session.SetSwimDive(swimming && duck);
                if (jump && session.Jump())
                    Play(swimming && bossClips != null ? bossClips[(int)BossSoundKind.Splash] : jumpSound);
                if (skill && session.TryUseSkill()) Play(skillSound);
                if (shoot && canShoot && Time.unscaledTime >= nextShotSound)
                {
                    Play(shootSound);
                    nextShotSound = Time.unscaledTime + 0.22f;
                }
            }
            else
            {
                // Cinematic movement is owned by core. No input held before a
                // cutscene, pause or modal may leak into flight/combat afterwards.
                session.SetShooting(false);
                session.SetFlightLift(false);
                session.SetSwimLift(false);
                session.SetSwimDive(false);
                session.SetHorizontal(0f);
                session.SetDucking(false);
            }
            int oldStars = session.RunStars;
            int oldLives = session.Lives;
            RunState oldState = session.State;
            session.Update(Mathf.Min(Time.unscaledDeltaTime, 0.1f));
            if (session.State == RunState.Playing && !session.IsBossArena)
            {
                if (thunderStage != session.StageNumber)
                {
                    thunderStage = session.StageNumber;
                    lastThunderStrike = 0;
                }
                LightningFrame lightning = RunnerWeather.GetLightning(session.CurrentDistrict, session.DistrictElapsed);
                if (lightning.Amount > 0f && lightning.StrikeId > lastThunderStrike)
                {
                    lastThunderStrike = lightning.StrikeId;
                    Play(thunderSound);
                }
            }
            UpdateBossAudio();
            if (session.RunStars > oldStars) Play(coinSound);
            if (session.Lives < oldLives) Play(hurtSound);
            if (session.Lives > oldLives && oldState == RunState.Playing) Play(buySound);
            if (oldState != RunState.GameOver && session.State == RunState.GameOver) FlushSave();
            if (dirty && (session.State == RunState.Paused ||
                Time.unscaledTime >= nextSave && CanCheckpointNow())) FlushSave();
        }

        private static void ReadPointers(ref float horizontal, ref bool jump, ref bool duck)
        {
            bool shoot = false, skill = false;
            ReadCombatPointers(false, false, ref horizontal, ref jump, ref duck, ref shoot, ref skill);
        }

        private static void ReadCombatPointers(bool canShoot, bool canSkill,
            ref float horizontal, ref bool jump, ref bool duck, ref bool shoot, ref bool skill)
        {
            bool lift = false;
            ReadGameplayPointers(canShoot, canSkill, false,
                ref horizontal, ref jump, ref duck, ref shoot, ref skill, ref lift);
        }

        private static void ReadGameplayPointers(bool canShoot, bool canSkill, bool flying,
            ref float horizontal, ref bool jump, ref bool duck, ref bool shoot, ref bool skill, ref bool lift)
        {
            ReadMovementPointers(canShoot, canSkill, flying, false,
                ref horizontal, ref jump, ref duck, ref shoot, ref skill, ref lift);
        }

        private static void ReadMovementPointers(bool canShoot, bool canSkill, bool freeMovement, bool swimming,
            ref float horizontal, ref bool jump, ref bool duck, ref bool shoot, ref bool skill, ref bool lift)
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
                    ApplyMovementPointer(touch.position.ReadValue(), down, held, canShoot, canSkill, freeMovement, swimming,
                        ref horizontal, ref jump, ref duck, ref shoot, ref skill, ref lift);
                }
            }
            if (!activeTouch && Mouse.current != null)
                ApplyMovementPointer(Mouse.current.position.ReadValue(), Mouse.current.leftButton.wasPressedThisFrame,
                    Mouse.current.leftButton.isPressed, canShoot, canSkill, freeMovement, swimming,
                    ref horizontal, ref jump, ref duck, ref shoot, ref skill, ref lift);
        }

        private static void ApplyPointer(Vector2 raw, bool down, bool held, ref float horizontal, ref bool jump, ref bool duck)
        {
            bool shoot = false, skill = false;
            ApplyCombatPointer(raw, down, held, false, false,
                ref horizontal, ref jump, ref duck, ref shoot, ref skill);
        }

        private static void ApplyCombatPointer(Vector2 raw, bool down, bool held, bool canShoot, bool canSkill,
            ref float horizontal, ref bool jump, ref bool duck, ref bool shoot, ref bool skill)
        {
            bool lift = false;
            ApplyGameplayPointer(raw, down, held, canShoot, canSkill, false,
                ref horizontal, ref jump, ref duck, ref shoot, ref skill, ref lift);
        }

        private static void ApplyGameplayPointer(Vector2 raw, bool down, bool held,
            bool canShoot, bool canSkill, bool flying, ref float horizontal, ref bool jump,
            ref bool duck, ref bool shoot, ref bool skill, ref bool lift)
        {
            ApplyMovementPointer(raw, down, held, canShoot, canSkill, flying, false,
                ref horizontal, ref jump, ref duck, ref shoot, ref skill, ref lift);
        }

        private static void ApplyMovementPointer(Vector2 raw, bool down, bool held,
            bool canShoot, bool canSkill, bool freeMovement, bool swimming, ref float horizontal, ref bool jump,
            ref bool duck, ref bool shoot, ref bool skill, ref bool lift)
        {
            float scale = Mathf.Min(Screen.width / CanvasWidth, Screen.height / CanvasHeight);
            Vector2 offset = new Vector2((Screen.width - CanvasWidth * scale) * 0.5f, (Screen.height - CanvasHeight * scale) * 0.5f);
            Vector2 point = (new Vector2(raw.x, Screen.height - raw.y) - offset) / Mathf.Max(scale, 0.001f);
            if (point.x < 0 || point.x > CanvasWidth || point.y <= 130 || point.y >= 710) return;
            if (canShoot && ShootButton.Contains(point)) { if (held) shoot = true; }
            else if (canSkill && SkillButton.Contains(point)) { if (down) skill = true; }
            else if (MoveLeftButton.Contains(point)) { if (held) horizontal -= 1; }
            else if (MoveRightButton.Contains(point)) { if (held) horizontal += 1; }
            else if (DuckButton.Contains(point)) { if ((!freeMovement || swimming) && held) duck = true; }
            else
            {
                if (down) jump = true;
                if (freeMovement && held) lift = true;
            }
        }

        private void BeginRun()
        {
            StopBossAudio(false);
            showShop = showHistory = showCharacters = showPractice = false;
            bestAtStart = session.Save.BestScore;
            thunderStage = lastThunderStrike = 0;
            session.StartRun();
            // Consume purchased stock durably before the active running frames.
            FlushSave();
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
                    GUI.enabled = !ModalOpen;
                    DrawMenu();
                    GUI.enabled = true;
                    break;
                case RunState.Playing:
                    GUI.enabled = !ModalOpen;
                    DrawHud();
                    DrawPlayHints();
                    GUI.enabled = true;
                    break;
                case RunState.Paused:
                    GUI.enabled = false;
                    DrawHud();
                    GUI.enabled = !ModalOpen;
                    DrawPaused();
                    GUI.enabled = true;
                    break;
                case RunState.GameOver:
                    GUI.enabled = false;
                    DrawHud();
                    GUI.enabled = !ModalOpen;
                    DrawResults();
                    GUI.enabled = true;
                    break;
            }
            if (showShop) DrawShop();
            if (showHistory) DrawHistory();
            if (showCharacters) DrawCharacters();
            if (showPractice) DrawPractice();
            GUI.matrix = previous;
            GUI.color = Color.white;
        }

        private void DrawMenu()
        {
            Box(new Rect(64, 56, 486, 602), cream);
            Text(new Rect(98, 85, 390, 24), "●  ENDLESS RUNNER", 15, soft, FontStyle.Bold);
            Text(new Rect(94, 151, 420, 88), "DINO CITY", 64, ink, FontStyle.Bold);
            Text(new Rect(98, 250, 402, 62), "Canh khe tên lửa, né chuỗi đòn phối hợp.\nBoss đầu cấp 3 · 30k: lặn đấu tàu ngầm.", 17, soft);
            Stat(new Rect(98, 330, 124, 64), "KỶ LỤC", session.Save.BestScore.ToString("N0"));
            Stat(new Rect(238, 330, 124, 64), "LƯỢT TRƯỚC", session.Save.LastScore.ToString("N0"));
            Stat(new Rect(378, 330, 124, 64), "VÍ SAO", session.Save.Wallet.ToString("N0"));
            if (Button(new Rect(98, 408, 418, 39), "ĐẤU TẬP BOSS", ink, cream, 18)) OpenPractice();
            if (Button(new Rect(98, 458, 418, 62), "BẮT ĐẦU CHẠY     →", mint, ink, 22)) BeginRun();
            if (Button(new Rect(98, 534, 130, 52), "Nhân vật", ink, cream, 16)) OpenCharacters();
            if (Button(new Rect(242, 534, 130, 52), "Cửa hàng", ink, cream, 16)) showShop = true;
            if (Button(new Rect(386, 534, 130, 52), "Thành tích", new Color32(231, 231, 215, 255), ink, 16)) showHistory = true;
            Text(new Rect(98, 606, 420, 24), "BẢN 15  ·  SPACE để bắt đầu  ·  Có cảm ứng", 14, soft);

            Box(new Rect(782, 78, 408, 47), new Color(1, 1, 1, 0.82f));
            Text(new Rect(800, 90, 372, 24), RunnerSession.CharacterName(session.Save.SelectedCharacter) + " · SẴN SÀNG ĐẤU BOSS", 16, ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            DrawMenuDino();
            DrawMapSelection();
            Box(new Rect(806, 586, 376, 62), ink);
            Icon(art.Heart, new Rect(824, 602, 28, 26));
            Text(new Rect(866, 598, 304, 36), "Lượt tới: " + (1 + session.Save.PurchasedHearts) + " mạng", 17, cream, FontStyle.Bold, TextAnchor.MiddleLeft);
            if (Button(new Rect(1152, 654, 72, 36), muted ? "Âm: tắt" : "Âm: bật", cream, ink, 13)) ToggleSound();
        }

        private void DrawMenuDino()
        {
            int frame = Mathf.FloorToInt(ambientTime * 7) % 5;
            float y = 136 + Mathf.Sin(ambientTime * 2) * 5;
            if (session != null && session.Save.SelectedCharacter != CharacterKind.Classic)
            {
                world.DrawCharacterPreview(session.Save.SelectedCharacter, frame, new Rect(900, y, 164, 151));
                return;
            }
            if (art.DinoRunAtlas != null)
            {
                // Match the gameplay body's aspect rather than squeezing the
                // wider atlas pose into the old portrait preview rectangle.
                Rect body = new Rect(900, y, 164, 151);
                GUI.DrawTextureWithTexCoords(art.GetDinoRunRect(frame, body),
                    art.DinoRunAtlas, art.GetDinoRunUV(frame), true);
            }
            else if (art.DinoFrames != null && art.DinoFrames.Length > 0)
            {
                Texture2D texture = art.DinoFrames[frame % art.DinoFrames.Length];
                if (texture != null)
                    GUI.DrawTexture(new Rect(900, y, 140, 151), texture, ScaleMode.ScaleToFit);
            }
        }

        private void DrawMapSelection()
        {
            Box(new Rect(758, 296, 452, 274), cream);
            Text(new Rect(780, 310, 408, 24), "CHỌN MAP KHỞI ĐẦU", 16, ink, FontStyle.Bold);
            MapButton(new Rect(780, 343, 408, 38), null, "Ngẫu nhiên mỗi lượt");
            MapButton(new Rect(780, 389, 198, 38), District.Day, "Bình minh");
            MapButton(new Rect(990, 389, 198, 38), District.Snow, "Phố tuyết");
            MapButton(new Rect(780, 433, 198, 38), District.Sunset, "Nắng gắt");
            MapButton(new Rect(990, 433, 198, 38), District.Night, "Phố đêm");
            MapButton(new Rect(780, 477, 198, 38), District.Skyway, "Trên mây");
            MapButton(new Rect(990, 477, 198, 38), District.Storm, "Giông bão");
            string challenge = session.StartingDistrict.HasValue
                ? RunnerSession.DistrictChallenge(session.StartingDistrict.Value)
                : "Mở đầu bất kỳ map nào trong 6 map.";
            Text(new Rect(780, 530, 408, 18), challenge, 13, soft);
            Text(new Rect(780, 550, 408, 18), "Xáo đủ 6 map mỗi vòng · Đổi chặng sau 35 giây.", 12, soft);
        }

        private void MapButton(Rect rect, District? district, string caption)
        {
            bool selected = session.StartingDistrict == district;
            if (Button(rect, (selected ? "●  " : "") + caption,
                selected ? mint : new Color32(231, 231, 215, 255), ink, 16)
                && session.SetStartingDistrict(district))
            {
                PlayerPrefs.SetInt(StartingDistrictKey, district.HasValue ? (int)district.Value : -1);
                PlayerPrefs.Save();
            }
        }

        private void DrawHud()
        {
            // IMGUI can visit Layout/input/Repaint in one frame. Numeric labels
            // reuse their strings until the visible value actually changes.
            int best = Math.Max(session.Score, session.Save.BestScore);
            if (hudScore != session.Score) { hudScore = session.Score; hudScoreText = hudScore.ToString("D5"); }
            if (hudBest != best) { hudBest = best; hudBestText = hudBest.ToString("D5"); }
            if (hudStars != session.RunStars) { hudStars = session.RunStars; hudStarsText = "+" + hudStars; }
            if (hudWallet != session.Save.Wallet) { hudWallet = session.Save.Wallet; hudWalletText = "Ví: " + hudWallet; }
            Box(new Rect(36, 26, 1208, 86), cream);
            Text(new Rect(58, 43, 170, 19), "DINO CITY", 16, ink, FontStyle.Bold);
            Text(new Rect(58, 70, 180, 23), session.IsPractice ? "ĐẤU TẬP · " + (session.PracticeLearning ? "CƠ BẢN" : "CHUẨN") : session.IsBossArena ? "ĐẤU TRƯỜNG · " + session.BossNumber : DistrictName(session.CurrentDistrict), 14, soft);
            Text(new Rect(246, 40, 180, 21), session.IsPractice ? "LƯỢT TẬP" : session.IsBossArena ? "ĐIỂM · TẠM DỪNG" : "ĐIỂM", 12, soft, FontStyle.Bold);
            Text(new Rect(244, 60, 182, 39), session.IsPractice ? session.PracticeElapsed.ToString("0.0") + " s" : hudScoreText, 30, ink, FontStyle.Bold);
            Text(new Rect(452, 40, 155, 21), session.IsPractice ? "TRÚNG ĐÒN" : "KỶ LỤC", 12, soft, FontStyle.Bold);
            Text(new Rect(450, 65, 168, 29), session.IsPractice ? session.PracticeHits.ToString() : hudBestText, 22, ink, FontStyle.Bold);
            for (int i = 0; i < 5; i++)
            {
                Color previous = GUI.color;
                GUI.color = i < session.Lives ? Color.white : new Color(0.55f, 0.6f, 0.6f, 0.24f);
                Icon(art.Heart, new Rect(660 + i * 30, 55, 26, 26));
                GUI.color = previous;
            }
            Icon(art.Star, new Rect(852, 48, 32, 32));
            Text(new Rect(897, 43, 124, 26), session.IsPractice ? "ĐẤU TẬP" : hudStarsText, session.IsPractice ? 18 : 24, ink, FontStyle.Bold);
            Text(new Rect(897, 75, 150, 20), hudWalletText, 13, soft);
            if (Button(new Rect(1070, 43, 54, 50), muted ? "♫ ×" : "♫", new Color32(231, 231, 215, 255), ink, 18)) ToggleSound();
            if (Button(new Rect(1136, 43, 82, 50), "II", ink, cream, 22)) session.TogglePause();
            DrawRunResources();
        }

        private void DrawRunResources()
        {
            if (session.IsBossArena)
            {
                DrawBossHud();
                return;
            }
            if (session.CurrentDistrict == District.Snow || session.CurrentDistrict == District.Sunset)
            {
                bool cold = session.CurrentDistrict == District.Snow;
                float value = cold ? session.Warmth : session.Water;
                bool reserve = cold ? session.WarmReserveReady : session.WaterReserveReady;
                float recovery = cold ? session.WarmthRecoveryRemaining : session.WaterRecoveryRemaining;
                Box(new Rect(36, 128, 276, 63 + (reserve ? 20 : 0) + (recovery > 0 ? 20 : 0)), cream);
                PickupIcon(cold ? EntityKind.HotDrink : EntityKind.Water, new Rect(49, 139, 32, 38));
                Text(new Rect(92, 139, 205, 21), (cold ? "THÂN NHIỆT " : "NƯỚC ") + Mathf.CeilToInt(value) + "%", 14, ink, FontStyle.Bold);
                Box(new Rect(92, 165, 202, 9), new Color32(217, 224, 221, 255));
                if (value > 0)
                    Box(new Rect(92, 165, 202 * Mathf.Clamp01(value / 100f), 9), value < 25 ? new Color32(222, 87, 69, 255) : cold ? new Color32(240, 150, 76, 255) : new Color32(68, 172, 213, 255));
                float row = 183;
                if (recovery > 0)
                {
                    Text(new Rect(49, row, 250, 22), "Đang hồi dần · +" + Mathf.CeilToInt(recovery), 12, soft, FontStyle.Bold);
                    row += 20;
                }
                if (reserve) Text(new Rect(49, row, 250, 22), cold ? "Có bình giữ ấm dự phòng" : "Có nước dự phòng", 12, soft, FontStyle.Bold);
            }
            string effects = "";
            if (session.ScoreBoostRemaining > 0) effects += "×2 ĐIỂM  " + session.ScoreBoostRemaining.ToString("0.0") + " s\n";
            if (session.MagnetRemaining > 0) effects += "NAM CHÂM  " + session.MagnetRemaining.ToString("0.0") + " s\n";
            if (session.ShieldReady) effects += "KHIÊN SẴN SÀNG";
            if (effects.Length > 0)
            {
                Box(new Rect(964, 128, 280, 77), ink);
                Text(new Rect(980, 138, 250, 64), effects.TrimEnd(), 14, cream, FontStyle.Bold);
            }
        }

        private void DrawPlayHints()
        {
            bool ready = !session.IsBossArena || session.ArenaPhase == BossPhase.Fighting;
            bool flying = session.IsFlyingBoss;
            bool swimming = session.IsUnderwaterBoss;
            Color disabled = new Color32(193, 204, 196, 255);
            Box(MoveLeftButton, ready ? cream : disabled);
            Text(MoveLeftButton, "←", 30, ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            Box(MoveRightButton, ready ? cream : disabled);
            Text(MoveRightButton, "→", 30, ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            bool showShoot = session.IsBossArena;
            bool showSkill = session.CurrentCharacter != CharacterKind.Classic;
            float hintWidth = showShoot ? 466f : showSkill ? 596f : 728f;
            Box(new Rect(242, 632, hintWidth, 56), new Color(0.06f, 0.13f, 0.18f, 0.88f));
            string hint = !ready ? "Đang chuyển cảnh · ESC: tạm dừng\nChờ khủng long đến đấu trường"
                : swimming ? "← → / A D: bơi ngang · J: giữ để bắn\nSPACE / ↑ / W: bơi lên · ↓ / S: lặn xuống" + (showSkill ? " · E: chiêu" : "")
                : flying ? "← → / A D: di chuyển · J: giữ để bắn\nSPACE / ↑ / W: giữ để bay, thả để hạ" + (showSkill ? " · E: chiêu" : "")
                : showShoot
                ? "← → / A D: di chuyển · SPACE / ↑: nhảy kép\n↓ / S: cúi · J: giữ để bắn" + (showSkill ? " · E: chiêu" : "")
                : "← → / A D: di chuyển · SPACE / ↑: nhảy kép\n↓ / S: cúi · ESC: tạm dừng" + (showSkill ? " · E: chiêu" : "");
            Text(new Rect(260, 640, hintWidth - 32, 41), hint, showShoot ? 13 : showSkill ? 14 : 15, cream);
            if (showShoot)
            {
                Box(ShootButton, ready ? mint : disabled);
                Text(ShootButton, "BẮN · J", 19, ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            }
            if (showSkill) DrawSkillControl();
            if (!flying || swimming)
            {
                Box(DuckButton, ready ? cream : disabled);
                Text(DuckButton, swimming ? "LẶN ↓" : "CÚI ↓", 19, ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            }
            Box(JumpButton, ready ? mint : disabled);
            Text(JumpButton, swimming ? "BƠI ↑" : flying ? "BAY ↑" : "NHẢY ↑", 19, ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            Text(new Rect(38, 592, 680, 28), session.IsPractice
                ? "ĐẤU TẬP · Không thưởng sao, không ghi kỷ lục · Né dấu báo trước đòn"
                : session.IsBossArena
                ? "Hạ boss để tiếp tục hành trình · Điểm và hiệu ứng đang được giữ"
                : RunningHint(),
                14, cream, FontStyle.Bold);
            if (session.InvincibleRemaining > 0)
                Text(new Rect(726, 592, 250, 28), "Bảo vệ: " + session.InvincibleRemaining.ToString("0.0") + " s", 14, mint, FontStyle.Bold);
        }

        private string RunningHint()
        {
            int metres = Mathf.FloorToInt(session.Distance / 20f);
            int speed = (int)Math.Round(session.Speed / RunnerSession.InitialSpeed * 100d);
            if (runningHint == null || metres != hintMetres || speed != hintSpeedHundredths || session.NextBossScore != hintBossScore)
            {
                hintMetres = metres; hintSpeedHundredths = speed; hintBossScore = session.NextBossScore;
                runningHint = metres + " m  ·  " + (speed / 100f).ToString("0.00") + "×  ·  Boss: " + hintBossScore.ToString("N0");
            }
            return runningHint;
        }

        private void DrawSkillControl()
        {
            bool active = session.SkillActiveRemaining > 0;
            string name = session.CurrentCharacter == CharacterKind.Triceratops ? "GIÁP" : "LƯỚT";
            string caption = active ? name + "\n" + session.SkillActiveRemaining.ToString("0.0") + " s"
                : session.SkillCooldownRemaining > 0 ? "CHIÊU · E\n" + Mathf.CeilToInt(session.SkillCooldownRemaining) + " s" : name + " · E";
            bool controlsReady = !session.IsBossArena || session.ArenaPhase == BossPhase.Fighting;
            Box(SkillButton, !controlsReady ? new Color32(193, 204, 196, 255)
                : active ? new Color32(145, 215, 230, 255) : session.SkillReady ? mint : new Color32(210, 218, 207, 255));
            Text(SkillButton, caption, 16, ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            float progress = session.SkillCooldown > 0 ? 1f - session.SkillCooldownRemaining / session.SkillCooldown : 0;
            Box(new Rect(SkillButton.x + 5, SkillButton.yMax - 7, (SkillButton.width - 10) * Mathf.Clamp01(progress), 3), ink);
        }

        private void DrawBossHud()
        {
            BossEncounter boss = session.CurrentBoss;
            if (boss == null) return;
            Box(new Rect(36, 128, 1208, 77), ink);
            string name = PracticeBossName(boss.Kind);
            Text(new Rect(58, 143, 263, 25), name, 19, cream, FontStyle.Bold);
            Box(new Rect(340, 148, 664, 17), new Color32(61, 78, 84, 255));
            float fraction = boss.MaxHealth > 0 ? Mathf.Clamp01((float)boss.Health / boss.MaxHealth) : 0f;
            if (fraction > 0)
                Box(new Rect(340, 148, 664 * fraction, 17), boss.Vulnerable
                    ? boss.RageLevel >= 3 ? new Color32(246, 100, 78, 255) : new Color32(239, 153, 93, 255)
                    : new Color32(130, 165, 183, 255));
            Text(new Rect(1028, 142, 190, 27), Mathf.CeilToInt(boss.Health) + " / " + Mathf.CeilToInt(boss.MaxHealth), 17, cream, FontStyle.Bold, TextAnchor.MiddleRight);
            string phase = "PHA " + boss.RageLevel + (boss.RageLevel >= 3 ? " · CUỒNG NỘ" : "");
            string status = "CẤP " + session.BossTier + " · " + (session.ArenaPhase == BossPhase.Intro ? BossJourneyHint()
                : session.ArenaPhase == BossPhase.Victory ? session.IsUnderwaterBoss ? "ĐÃ HẠ BOSS · NỔI LÊN VÀ QUA CỔNG PHÍA TRƯỚC" : session.IsFlyingBoss ? "ĐÃ HẠ BOSS · HẠ CÁNH VÀ QUA CỔNG PHÍA TRƯỚC" : "ĐÃ HẠ BOSS · TIẾN QUA CỔNG PHÍA TRƯỚC"
                : phase + " · " + boss.AttackName + (boss.Vulnerable
                    ? " · LÕI HỞ: GIỮ J HOẶC NÚT BẮN" : " · CANH DẤU BÁO VÀ NÉ ĐÒN"));
            Text(new Rect(58, 177, 1158, 18), status, 12, cream, FontStyle.Bold, TextAnchor.MiddleCenter);
            if (session.IsPractice && session.PracticeLearning && session.ArenaPhase == BossPhase.Fighting)
            {
                Box(new Rect(334, 215, 612, 42), new Color(0.06f, 0.13f, 0.18f, 0.92f));
                Text(new Rect(346, 226, 588, 22), session.IsUnderwaterBoss
                    ? "Giữ SPACE / BƠI để lên · ↓ / S / LẶN để xuống · J để bắn"
                    : session.IsFlyingBoss
                    ? "Giữ SPACE / BAY để lên · Thả để xuống · Nhấn theo nhịp để giữ độ cao"
                    : "Dấu báo xuất hiện trước đòn · Bắn vỡ vật cản, nhảy hoặc cúi để né",
                    13, cream, FontStyle.Normal, TextAnchor.MiddleCenter);
            }
        }

        private string BossJourneyHint()
        {
            switch (session.TransitionStage)
            {
                case BossTransitionStage.Collect: return session.IsUnderwaterBoss
                    ? "ĐỘI MŨ LẶN · CHUẨN BỊ XUỐNG NƯỚC" : "NHẶT VÁN PHẢN LỰC · CHUẨN BỊ CẤT CÁNH";
                case BossTransitionStage.Takeoff: return "ĐANG CẤT CÁNH · SẮP ĐẾN TRẬN KHÔNG CHIẾN";
                case BossTransitionStage.Diving: return "ĐANG LẶN XUỐNG · CHUẨN BỊ ĐẤU TÀU NGẦM";
                case BossTransitionStage.Surfacing: return "NỔI LÊN MẶT NƯỚC · TIẾN QUA CỔNG PHÍA TRƯỚC";
                case BossTransitionStage.Arrive: return session.IsUnderwaterBoss
                    ? "SPACE / BƠI: LÊN · ↓ / LẶN: XUỐNG · J: BẮN"
                    : session.IsFlyingBoss
                    ? "GIỮ SPACE / BAY ĐỂ LÊN · THẢ ĐỂ HẠ · J ĐỂ BẮN"
                    : "SẮP GIAO CHIẾN · J ĐỂ BẮN · CANH DẤU BÁO ĐỂ NÉ";
                default: return session.IsUnderwaterBoss ? "TIẾN ĐẾN MŨ LẶN · CHUẨN BỊ LẶN" : session.IsFlyingBoss ? "TIẾN ĐẾN VÁN PHẢN LỰC" : "ĐANG TIẾN VÀO ĐẤU TRƯỜNG";
            }
        }

        private void DrawPaused()
        {
            Scrim();
            Box(new Rect(410, 182, 460, 370), cream);
            Text(new Rect(442, 213, 396, 26), "NGHỈ CHÂN MỘT CHÚT", 15, soft, FontStyle.Bold, TextAnchor.MiddleCenter);
            Text(new Rect(442, 252, 396, 56), "Tạm dừng", 40, ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            Text(new Rect(448, 321, 384, 30), session.IsPractice ? "Tiếp tục làm quen với các chiêu boss." : "Chuyến đi của bạn vẫn đang chờ.", 17, soft, FontStyle.Normal, TextAnchor.MiddleCenter);
            if (Button(new Rect(448, 381, 384, 59), "TIẾP TỤC    →", mint, ink, 21)) session.TogglePause();
            if (Button(new Rect(448, 455, 384, 48), session.IsPractice ? "Dừng tập và về menu" : "Kết thúc lượt và về menu", new Color32(231, 231, 215, 255), ink, 17)) GoToMenu();
        }

        private void DrawResults()
        {
            if (session.IsPractice) { DrawPracticeResults(); return; }
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
            if (Button(new Rect(414, 550, 452, 50), "Về menu & mua tim", ink, cream, 18)) GoToMenu();
            Text(new Rect(414, 613, 452, 22), "R: chơi lại · Map đầu: " + (session.StartingDistrict.HasValue
                ? DistrictName(session.StartingDistrict.Value) : "ngẫu nhiên"), 13, soft, FontStyle.Normal, TextAnchor.MiddleCenter);
        }

        private string EndMessage()
        {
            if (session.IsBossArena)
                return "Giữ J để bắn, né đòn và thử lại nhé!";
            if (session.EndReason != null && session.EndReason.IndexOf("thân nhiệt", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Nhặt đồ uống nóng để giữ thân nhiệt nhé!";
            if (session.EndReason != null && session.EndReason.IndexOf("nước", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Nhặt chai nước để vượt qua nắng gắt nhé!";
            if (session.EndReason != null && session.EndReason.IndexOf("Rơi", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Lỡ một nhịp nhảy. Thử lại nhé!";
            if (session.EndReason != null && session.EndReason.IndexOf("máy bay", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Máy bay bay thấp: giữ ↓ hoặc nút CÚI để tránh.";
            return "Hết mạng rồi. Mua thêm tim cho lượt tiếp nhé!";
        }

        private void OpenPractice()
        {
            if (session.IsPractice && session.CurrentBoss != null)
            {
                practiceBoss = session.CurrentBoss.Kind;
                practiceCharacter = session.CurrentCharacter;
                practiceLearning = session.PracticeLearning;
                practiceTier = session.BossTier;
                practiceSettingsInitialized = true;
            }
            else if (!practiceSettingsInitialized)
            {
                practiceCharacter = session.Save.SelectedCharacter;
                practiceSettingsInitialized = true;
            }
            showShop = showHistory = showCharacters = false;
            showPractice = true;
        }

        private void StartPractice()
        {
            StopBossAudio(false);
            if (!session.StartBossPractice(practiceBoss, practiceCharacter, practiceLearning, practiceTier)) return;
            practiceSettingsInitialized = true;
            showShop = showHistory = showCharacters = showPractice = false;
            thunderStage = lastThunderStrike = 0;
            nextShotSound = 0f;
        }

        private void RetryPractice()
        {
            if (!session.IsPractice || session.CurrentBoss == null) return;
            practiceBoss = session.CurrentBoss.Kind;
            practiceCharacter = session.CurrentCharacter;
            practiceLearning = session.PracticeLearning;
            practiceTier = session.BossTier;
            StartPractice();
        }

        private static string PracticeBossName(BossKind kind)
        {
            switch (kind)
            {
                case BossKind.Tank: return "XE TĂNG K-01";
                case BossKind.Gunship: return "TRỰC THĂNG H-03";
                case BossKind.Submarine: return "TÀU NGẦM N-04";
                default: return "VỆ BINH G-02";
            }
        }

        private void DrawPractice()
        {
            Scrim();
            Box(new Rect(70, 34, 1140, 652), cream);
            Text(new Rect(102, 56, 990, 32), "ĐẤU TẬP BOSS", 27, ink, FontStyle.Bold);
            if (Button(new Rect(1148, 51, 40, 38), "×", new Color32(231, 231, 215, 255), ink, 23)) showPractice = false;
            Text(new Rect(102, 101, 146, 23), "CẤP BOSS: " + practiceTier, 15, ink, FontStyle.Bold);
            int[] tiers = { 1, 2, 3, 5, 8 };
            for (int i = 0; i < tiers.Length; i++)
            {
                int tier = tiers[i];
                bool selected = practiceTier == tier;
                if (Button(new Rect(258 + i * 88, 96, 80, 29), (selected ? "● " : "") + "CẤP " + tier,
                    selected ? mint : new Color32(231, 231, 215, 255), ink, 13)) practiceTier = tier;
            }
            Text(new Rect(716, 101, 462, 23), "Cấp cao: thêm đợt chiêu, đạn nhanh và khó né hơn.", 13, soft);
            DrawPracticeBoss(new Rect(102, 138, 252, 181), BossKind.Tank,
                "Pháo, drone, húc và quét sân.");
            DrawPracticeBoss(new Rect(376, 138, 252, 181), BossKind.Gunship,
                "Cổng điện, đạn tỏa, mìn bay.");
            DrawPracticeBoss(new Rect(650, 138, 252, 181), BossKind.Submarine,
                "Ngư lôi, đàn cá máy, thủy lôi.");
            DrawPracticeBoss(new Rect(924, 138, 252, 181), BossKind.Sentinel,
                "Laser, hồ quang, lao đấm.");
            Text(new Rect(102, 332, 1060, 25), "NHÂN VẬT · Có thể thử cả nhân vật chưa mở khóa", 15, ink, FontStyle.Bold);
            DrawPracticeCharacter(new Rect(102, 369, 344, 130), CharacterKind.Classic);
            DrawPracticeCharacter(new Rect(468, 369, 344, 130), CharacterKind.Triceratops);
            DrawPracticeCharacter(new Rect(834, 369, 344, 130), CharacterKind.Raptor);
            if (Button(new Rect(102, 519, 344, 43), (practiceLearning ? "●  " : "") + "LÀM QUEN", practiceLearning ? mint : new Color32(231, 231, 215, 255), ink, 18)) practiceLearning = true;
            if (Button(new Rect(468, 519, 344, 43), (!practiceLearning ? "●  " : "") + "THỬ THÁCH CHUẨN", !practiceLearning ? mint : new Color32(231, 231, 215, 255), ink, 18)) practiceLearning = false;
            Text(new Rect(102, 575, 1060, 23), practiceLearning
                ? "5 tim · Boss ít máu hơn · Đòn chậm và báo trước lâu hơn để học cách né."
                : "3 tim · Theo cấp đã chọn · Đòn đánh và luật sát thương tiêu chuẩn.", 15, soft);
            if (Button(new Rect(834, 519, 344, 43), "VÀO ĐẤU TẬP    →", ink, cream, 19)) StartPractice();
            Text(new Rect(102, 605, 1060, 22), "Không tốn đồ đã mua, không cộng sao hoặc ghi thành tích. Có thể thử cả nhân vật chưa mở.", 13, soft);
            Text(new Rect(102, 640, 1060, 24), practiceBoss == BossKind.Submarine
                ? "← → / A D: bơi ngang · Giữ SPACE / BƠI để lên · ↓ / S / LẶN để xuống · J: bắn · E: kỹ năng"
                : practiceBoss == BossKind.Gunship
                ? "← → / A D: di chuyển · Giữ SPACE / BAY để lên, thả để hạ · J: giữ bắn · E: kỹ năng riêng"
                : "← → / A D: di chuyển · SPACE: nhảy kép · ↓ / S: cúi · J: giữ bắn · E: kỹ năng riêng", 14, ink, FontStyle.Normal, TextAnchor.MiddleCenter);
        }

        private void DrawPracticeBoss(Rect rect, BossKind kind, string description)
        {
            bool selected = practiceBoss == kind;
            Box(rect, selected ? new Color32(220, 237, 203, 255) : new Color32(232, 237, 222, 255));
            Text(new Rect(rect.x + 12, rect.y + 12, rect.width - 24, 24), PracticeBossName(kind), 17, ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            if (kind == BossKind.Submarine && art.SubmarineAtlas != null)
            {
                Rect image = new Rect(rect.center.x - 91f, rect.y + 52f, 182f, 68f);
                GUI.DrawTextureWithTexCoords(image, art.SubmarineAtlas, art.GetSubmarineUV(0), true);
            }
            else if (kind != BossKind.Submarine && art.BossAtlas != null)
            {
                Rect image = kind == BossKind.Sentinel ? new Rect(rect.center.x - 37f, rect.y + 43f, 74f, 79f)
                    : kind == BossKind.Gunship ? new Rect(rect.center.x - 73f, rect.y + 50f, 146f, 67f)
                    : new Rect(rect.center.x - 68f, rect.y + 56f, 136f, 62f);
                GUI.DrawTextureWithTexCoords(image, art.BossAtlas, art.GetBossUV(kind), true);
            }
            Text(new Rect(rect.x + 12, rect.y + 127, rect.width - 24, 21), description, 12, soft, FontStyle.Normal, TextAnchor.MiddleCenter);
            if (Button(new Rect(rect.x + 12, rect.yMax - 31, rect.width - 24, 24), selected ? "ĐANG CHỌN" : "CHỌN BOSS", selected ? mint : cream, ink, 12)) practiceBoss = kind;
        }

        private void DrawPracticeCharacter(Rect rect, CharacterKind kind)
        {
            bool selected = practiceCharacter == kind;
            Box(rect, selected ? new Color32(220, 237, 203, 255) : new Color32(232, 237, 222, 255));
            world.DrawCharacterPreview(kind, Mathf.FloorToInt(ambientTime * 7) % 5,
                new Rect(rect.x + 15, rect.y + 17, 100, 92));
            Text(new Rect(rect.x + 125, rect.y + 14, rect.width - 137, 25), RunnerSession.CharacterName(kind), 18, ink, FontStyle.Bold);
            string skill = kind == CharacterKind.Classic ? "Bắn · Không có kỹ năng riêng"
                : kind == CharacterKind.Triceratops ? "Bắn · E: giáp chắn một đòn"
                : "Bắn · E: lướt né đòn";
            Text(new Rect(rect.x + 125, rect.y + 47, rect.width - 137, 21), skill, 12, soft);
            if (Button(new Rect(rect.x + 125, rect.y + 81, rect.width - 139, 31), selected ? "ĐANG CHỌN" : session.OwnsCharacter(kind) ? "CHỌN" : "THỬ TRONG ĐẤU TẬP", selected ? mint : cream, ink, 12)) practiceCharacter = kind;
        }

        private void DrawPracticeResults()
        {
            Scrim();
            Box(new Rect(340, 122, 600, 534), cream);
            Text(new Rect(378, 150, 524, 26), "KẾT QUẢ ĐẤU TẬP", 16, soft, FontStyle.Bold, TextAnchor.MiddleCenter);
            Text(new Rect(378, 199, 524, 54), session.PracticeWon ? "Đã hạ boss!" : "Thử thêm một lượt!", 38, ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            Text(new Rect(378, 269, 524, 28), session.CurrentBoss == null ? "ĐẤU TẬP" : PracticeBossName(session.CurrentBoss.Kind) + " · CẤP " + session.BossTier, 18, soft, FontStyle.Bold, TextAnchor.MiddleCenter);
            Stat(new Rect(400, 327, 220, 75), "THỜI GIAN GIAO CHIẾN", session.PracticeElapsed.ToString("0.0") + " s");
            Stat(new Rect(658, 327, 220, 75), "SỐ LẦN TRÚNG ĐÒN", session.PracticeHits.ToString());
            Text(new Rect(378, 415, 524, 25), "Ví sao, kỷ lục và đồ cho lượt chạy được giữ nguyên.", 15, soft, FontStyle.Normal, TextAnchor.MiddleCenter);
            if (Button(new Rect(380, 462, 520, 54), "TẬP LẠI    →", mint, ink, 21)) RetryPractice();
            if (Button(new Rect(380, 530, 250, 46), "Đổi boss / nhân vật", ink, cream, 17)) OpenPractice();
            if (Button(new Rect(650, 530, 250, 46), "Về menu", new Color32(231, 231, 215, 255), ink, 17)) GoToMenu();
            Text(new Rect(380, 600, 520, 23), "R: tập lại · Có thể thử mọi nhân vật miễn phí", 13, soft, FontStyle.Normal, TextAnchor.MiddleCenter);
        }

        private void OpenCharacters()
        {
            showShop = showHistory = showPractice = false;
            showCharacters = true;
        }

        private void DrawCharacters()
        {
            Scrim();
            Box(new Rect(150, 70, 980, 590), cream);
            Text(new Rect(182, 96, 650, 31), "CHỌN BẠN ĐỒNG HÀNH", 25, ink, FontStyle.Bold);
            Icon(art.Star, new Rect(876, 96, 27, 27));
            Text(new Rect(916, 97, 139, 28), session.Save.Wallet.ToString("N0"), 19, ink, FontStyle.Bold, TextAnchor.MiddleRight);
            if (Button(new Rect(1070, 88, 40, 38), "×", new Color32(231, 231, 215, 255), ink, 23)) showCharacters = false;
            Text(new Rect(182, 143, 914, 26), "Mua một lần, mở khóa vĩnh viễn. Cả ba khủng long đều bắn được khi gặp boss.", 16, soft);
            DrawCharacterCard(new Rect(182, 188, 292, 380), CharacterKind.Classic);
            DrawCharacterCard(new Rect(494, 188, 292, 380), CharacterKind.Triceratops);
            DrawCharacterCard(new Rect(806, 188, 292, 380), CharacterKind.Raptor);
            Text(new Rect(182, 594, 914, 43), "J / nút BẮN: chỉ khi đấu boss · E / nút CHIÊU: kỹ năng riêng\nChọn nhân vật trước khi bắt đầu lượt. Tim và đồ tiếp tế mua riêng trong cửa hàng.", 14, soft, FontStyle.Normal, TextAnchor.MiddleCenter);
        }

        private void DrawCharacterCard(Rect rect, CharacterKind character)
        {
            bool owned = session.OwnsCharacter(character);
            bool selected = session.Save.SelectedCharacter == character;
            Box(rect, selected ? new Color32(220, 237, 203, 255) : new Color32(232, 237, 222, 255));
            Text(new Rect(rect.x + 18, rect.y + 17, rect.width - 36, 28), RunnerSession.CharacterName(character), 23, ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            world.DrawCharacterPreview(character, Mathf.FloorToInt(ambientTime * 7) % 5,
                new Rect(rect.x + 62, rect.y + 65, 168, 154));
            WrappedText(new Rect(rect.x + 20, rect.y + 235, rect.width - 40, 77),
                RunnerSession.CharacterSkillDescription(character), 15, soft);
            string caption = selected ? "ĐANG CHỌN" : owned ? "CHỌN NHÂN VẬT"
                : "MUA · " + RunnerSession.CharacterPrice(character).ToString("N0") + " SAO";
            bool canUse = !selected && (owned || session.Save.Wallet >= RunnerSession.CharacterPrice(character));
            if (Button(new Rect(rect.x + 18, rect.yMax - 51, rect.width - 36, 36), caption,
                canUse ? mint : new Color32(207, 219, 199, 255), ink, 16, canUse))
            {
                bool changed = owned ? session.SelectCharacter(character) : session.BuyCharacter(character);
                if (changed)
                {
                    Play(owned ? coinSound : buySound);
                    FlushSave();
                }
            }
        }

        private void WrappedText(Rect rect, string value, int size, Color color)
        {
            bool wasWrapped = label.wordWrap;
            label.wordWrap = true;
            Text(rect, value, size, color);
            label.wordWrap = wasWrapped;
        }

        private void DrawShop()
        {
            Scrim();
            Box(new Rect(216, 76, 848, 580), cream);
            Text(new Rect(246, 104, 500, 25), "CỬA HÀNG DINO CITY", 16, soft, FontStyle.Bold);
            Text(new Rect(246, 140, 530, 42), "Chuẩn bị chuyến đi.", 34, ink, FontStyle.Bold);
            Icon(art.Star, new Rect(788, 111, 31, 31));
            Text(new Rect(830, 112, 166, 32), session.Save.Wallet.ToString("N0"), 23, ink, FontStyle.Bold);
            if (Button(new Rect(1004, 96, 40, 38), "×", new Color32(231, 231, 215, 255), ink, 23)) showShop = false;
            if (Button(new Rect(246, 189, 374, 42), "TIM & TIẾP TẾ", shopTab == 0 ? mint : new Color32(231, 231, 215, 255), ink, 17)) shopTab = 0;
            if (Button(new Rect(638, 189, 374, 42), "NÂNG THỜI LƯỢNG", shopTab == 1 ? mint : new Color32(231, 231, 215, 255), ink, 17)) shopTab = 1;
            if (shopTab == 0)
            {
                ShopProduct(new Rect(246, 247, 374, 146), null,
                    "Tim khởi đầu · " + session.Save.PurchasedHearts + "/3",
                    "Chỉ dùng cho lượt kế tiếp.\nLượt tới: " + (1 + session.Save.PurchasedHearts) + " mạng.",
                    session.NextHeartCost, session.Save.PurchasedHearts < 3, session.BuyExtraHeart);
                ShopProduct(new Rect(638, 247, 374, 146), EntityKind.Shield,
                    "Khiên khởi đầu", "Chặn một lần va chạm.\nDùng cho lượt kế tiếp.",
                    RunnerSession.StartingShieldPrice, !session.Save.PurchasedShield, session.BuyStartingShield);
                ShopProduct(new Rect(246, 409, 374, 146), EntityKind.Water,
                    "Nước dự phòng", "Tự dùng khi thanh nước cạn.\nMột bình cho lượt kế tiếp.",
                    RunnerSession.SurvivalReservePrice, !session.Save.PurchasedWaterReserve, session.BuyWaterReserve);
                ShopProduct(new Rect(638, 409, 374, 146), EntityKind.HotDrink,
                    "Bình giữ ấm", "Tự dùng khi thân nhiệt cạn.\nMột bình cho lượt kế tiếp.",
                    RunnerSession.SurvivalReservePrice, !session.Save.PurchasedWarmReserve, session.BuyWarmReserve);
            }
            else
            {
                ShopProduct(new Rect(246, 247, 374, 308), EntityKind.DoubleScore,
                    "×2 điểm · Cấp " + session.Save.ScoreBoostLevel + "/4",
                    "Vật phẩm nhặt trên đường.\nĐang có: " + session.ScoreBoostDuration + " giây" +
                    (session.NextScoreBoostCost > 0 ? "\nNâng tiếp: " + (session.ScoreBoostDuration + 2) + " giây" : "\nĐã nâng tối đa.") +
                    "\nNhặt thêm để gia hạn.\nSao trong ví tính riêng.",
                    session.NextScoreBoostCost, session.NextScoreBoostCost > 0, session.BuyScoreBoostUpgrade);
                ShopProduct(new Rect(638, 247, 374, 308), EntityKind.Magnet,
                    "Nam châm · Cấp " + session.Save.MagnetLevel + "/4",
                    "Hút sao về phía khủng long.\nĐang có: " + session.MagnetDuration + " giây" +
                    (session.NextMagnetCost > 0 ? "\nNâng tiếp: " + (session.MagnetDuration + 2) + " giây" : "\nĐã nâng tối đa.") +
                    "\nNhặt nam châm trên đường\nđể kích hoạt hiệu ứng.",
                    session.NextMagnetCost, session.NextMagnetCost > 0, session.BuyMagnetUpgrade);
            }
            Text(new Rect(246, 583, 550, 49), shopTab == 0
                ? "Tim và tiếp tế dùng theo lượt. Nâng thời lượng áp dụng vĩnh viễn.\nTim nhặt trong màn hồi tối đa 5 mạng."
                : "Nâng thời lượng: 200 → 400 → 700 → 1.100 sao.\nVật phẩm được nhặt trong màn, tối đa 16 giây mỗi lần nhặt.",
                12, soft, FontStyle.Normal, TextAnchor.MiddleCenter);
            if (Button(new Rect(812, 588, 200, 38), "Mở nhân vật →", ink, cream, 16)) OpenCharacters();
        }

        private void ShopProduct(Rect rect, EntityKind? pickup, string title, string description,
            int price, bool available, Func<bool> purchase)
        {
            Box(rect, new Color32(232, 237, 222, 255));
            Rect iconRect = new Rect(rect.x + 16, rect.y + 14, 44, 42);
            if (pickup.HasValue) PickupIcon(pickup.Value, iconRect, pickup.Value == EntityKind.HotDrink);
            else Icon(art.Heart, iconRect);
            Text(new Rect(rect.x + 72, rect.y + 18, rect.width - 86, 31), title, 19, ink, FontStyle.Bold);
            Text(new Rect(rect.x + 16, rect.y + 61, rect.width - 32, rect.height - 104), description, 14, soft);
            bool canBuy = available && price > 0 && session.Save.Wallet >= price;
            string caption = !available ? (shopTab == 0 ? "ĐÃ CHUẨN BỊ" : "ĐÃ TỐI ĐA") : "MUA  ·  " + price.ToString("N0") + " SAO";
            if (Button(new Rect(rect.x + 16, rect.yMax - 42, rect.width - 32, 32), caption,
                canBuy ? mint : new Color32(215, 221, 209, 255), ink, 16, canBuy) && purchase())
            {
                Play(buySound);
                FlushSave();
            }
        }

        private void PickupIcon(EntityKind kind, Rect rect, bool warmReserve = false)
        {
            if (art.PickupAtlas == null) return;
            Color before = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTextureWithTexCoords(rect, art.PickupAtlas, warmReserve ? art.WarmReserveUV : art.GetPickupUV(kind), true);
            GUI.color = before;
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
            Text(new Rect(350, 598, 580, 23), "Điểm, sao và tim đã mua được lưu tự động trên thiết bị này.", 15, soft, FontStyle.Normal, TextAnchor.MiddleCenter);
        }

        private void GoToMenu()
        {
            StopBossAudio(false);
            session.ReturnToMenu();
            showShop = showHistory = showCharacters = showPractice = false;
            FlushSave();
        }

        private void SaveProgress()
        {
            // Picking up a row of stars only marks the latest live data dirty.
            // Do not allocate JSON for every star or postpone the deadline on
            // every pickup; serialize once when the batch is actually saved.
            if (dirty) return;
            dirty = true;
            nextSave = Time.unscaledTime + SaveCheckpointInterval;
        }
        private bool CanCheckpointNow()
        {
            if (session.State != RunState.Playing) return true;
            // Synchronous PlayerPrefs writes belong in a clear grounded gap,
            // never in the three-second map fade, a jump or a rocket approach.
            if (session.IsBossArena || session.DistrictElapsed < 3.2f || session.PlayerY > 1f ||
                session.PlayerY < -.5f || session.RunMissiles.Count > 0 || session.RunMissileWarnings.Count > 0) return false;
            float ahead = session.PlayerX + Math.Max(300f, session.Speed * 1.25f);
            for (int i = 0; i < session.Entities.Count; i++)
            {
                WorldEntity entity = session.Entities[i];
                if (entity.Removed || entity.Kind == EntityKind.Star || entity.Kind == EntityKind.Heart ||
                    entity.Kind == EntityKind.Water || entity.Kind == EntityKind.HotDrink ||
                    entity.Kind == EntityKind.DoubleScore || entity.Kind == EntityKind.Magnet || entity.Kind == EntityKind.Shield) continue;
                if (entity.X - entity.MotionAmplitudeX * 2f < ahead &&
                    entity.X + entity.Width + entity.MotionAmplitudeX * 2f > session.PlayerX - 75f) return false;
            }
            return true;
        }
        private void FlushSave()
        {
            if (!dirty || session == null) return;
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(session.Save));
            PlayerPrefs.Save();
            dirty = false;
        }
        private void OnApplicationFocus(bool focused)
        {
            if (focused || session == null) return;
            StopBossAudio(true);
            if (session.State == RunState.Playing) session.TogglePause();
            FlushSave();
        }
        private void OnApplicationPause(bool paused) { if (paused) OnApplicationFocus(false); }
        private void OnApplicationQuit()
        {
            StopBossAudio(true);
            if (session != null && (session.State == RunState.Playing || session.State == RunState.Paused)) session.ReturnToMenu();
            FlushSave();
        }
        private void OnDestroy()
        {
            DisposeBossAudio();
            if (session != null) session.SaveChanged -= SaveProgress;
            FlushSave();
            if (world != null) world.Dispose();
            if (roundTexture != null) Destroy(roundTexture);
            if (jumpSound != null) Destroy(jumpSound);
            if (coinSound != null) Destroy(coinSound);
            if (hurtSound != null) Destroy(hurtSound);
            if (buySound != null) Destroy(buySound);
            if (thunderSound != null) Destroy(thunderSound);
            if (shootSound != null) Destroy(shootSound);
            if (skillSound != null) Destroy(skillSound);
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
        private void ToggleSound()
        {
            muted = !muted;
            if (muted) StopBossAudio(true);
            PlayerPrefs.SetInt("CityDino.Muted", muted ? 1 : 0);
            PlayerPrefs.Save();
        }
        private void Play(AudioClip clip) { if (!muted && audioSource != null && clip != null) audioSource.PlayOneShot(clip); }
        private static AudioClip MakeThunder()
        {
            const int rate = 22050;
            var samples = new float[(int)(rate * 0.9f)];
            uint noise = 17431;
            float low = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                noise = unchecked(noise * 1664525u + 1013904223u);
                float random = ((noise >> 8) / 16777215f) * 2f - 1f;
                low += (random - low) * 0.08f;
                float seconds = (float)i / rate;
                float attack = Mathf.Min(1f, seconds * 80f);
                float tail = (float)Math.Exp(-seconds * 4f);
                samples[i] = (low * 0.85f + (float)Math.Sin(seconds * 2 * Math.PI * 48) * 0.12f)
                    * attack * tail;
            }
            AudioClip clip = AudioClip.Create("CityDino Thunder", samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }
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
                case District.Snow: return "Phố tuyết";
                case District.Sunset: return "Nắng gắt";
                case District.Night: return "Phố đêm";
                case District.Skyway: return "Thành phố trên mây";
                case District.Storm: return "Vùng giông";
                default: return "Bình minh";
            }
        }
    }
}
