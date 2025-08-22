using HarmonyLib;
using OWML.Common;
using OWML.ModHelper;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using System.Linq;
using System.IO;

namespace WebCameraBlink
{
    public class WebCameraBlink : ModBehaviour
    {
        public static WebCameraBlink Instance;

        public void Awake()
        {
            Instance = this;
        }

        public void Start()
        {
            ModHelper.Console.WriteLine($"{nameof(WebCameraBlink)} is loaded!", MessageType.Success);

            new Harmony("MegaPiggy.WebCameraBlink").PatchAll(Assembly.GetExecutingAssembly());
            
            var data = File.ReadAllBytes(WebCameraBlink.Instance.ModHelper.Manifest.ModFolderPath + "Outline.png");
            texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
            texture.name = "Outline";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.LoadImage(data);
            GameObject.DontDestroyOnLoad(texture);
            sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, Vector4.zero, false);
            sprite.name = texture.name;
            GameObject.DontDestroyOnLoad(texture);

            OnCompleteSceneLoad(OWScene.TitleScreen, OWScene.TitleScreen);
            LoadManager.OnCompleteSceneLoad += OnCompleteSceneLoad;
        }

        public void OnCompleteSceneLoad(OWScene previousScene, OWScene newScene)
        {
            ModHelper.Console.WriteLine("Loaded into " + newScene.ToString(), MessageType.Success);
            if (newScene == OWScene.TitleScreen) TitleScreen();
            else if (newScene == OWScene.SolarSystem || newScene == OWScene.EyeOfTheUniverse) SolarSystem();
        }

        public static float blinkingThreshhold = 0.4f;

        PlayerCameraEffectController cameraEffectController;

        float alarmStartTime;

        ShipAudioController shipAudioController;

        bool storeBlinkInput;

        InputMode previousInputMode; //Fixes sleeping while driving
        ScreenPrompt _wakePrompt;
        float _fastForwardMultiplier;
        float _fastForwardStartTime;

        public void CreatePipeReceiver()
        {
            new GameObject(nameof(PipeReceiver), typeof(PipeReceiver));
        }

        public void TitleScreen()
        {
            var canvas = new GameObject("CameraSelectionCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            var canvasComponent = canvas.GetComponent<Canvas>();
            canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1;
            SetSizeDelta(canvas, 1920, 1080);
            var margins = new GameObject("Margins", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ContentSizeFitter));
            margins.transform.SetParent(canvas.transform, false);
            SetImage(margins, Color.black);
            SetSizeDelta(margins, 1920, 1080);
            margins.transform.localPosition = new Vector3(0, 0, 0);
            var cameraSelectionGroup = new GameObject("CameraSelectionGroup", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(CanvasRenderer), typeof(Image), typeof(ContentSizeFitter));
            cameraSelectionGroup.transform.SetParent(margins.transform, false);
            FixVerticalLayoutGroup(cameraSelectionGroup);
            SetImage(cameraSelectionGroup, sprite);
            SetSizeDelta(cameraSelectionGroup, -70, 68.2212f);
            cameraSelectionGroup.transform.localPosition = new Vector3(0, 300, 0);
            var pleaseChooseYourDevice = new GameObject("PleaseChooseYourDevice", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Text), typeof(ContentSizeFitter));
            pleaseChooseYourDevice.transform.SetParent(cameraSelectionGroup.transform, false);
            SetTextAndFont(pleaseChooseYourDevice, "Please choose your device:");
            SetSizeDelta(pleaseChooseYourDevice, 615.01f, 65);
            var cameraSelect = new GameObject(nameof(CameraSelectUI), typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter), typeof(CameraSelectUI));
            cameraSelect.transform.SetParent(cameraSelectionGroup.transform, false);
            FixHorizontalLayoutGroup(cameraSelect);
            SetSizeDelta(cameraSelect, 773.9f, 65);
            var previousButton = new GameObject(nameof(CameraSelectUI.PreviousButton), typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CameraSelectUI.PreviousButton), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            previousButton.transform.SetParent(cameraSelect.transform, false);
            FixHorizontalLayoutGroup(previousButton);
            SetImage(previousButton, null);
            SetSizeDelta(previousButton, 72.72f, 65);
            var cameraText = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Text), typeof(ContentSizeFitter));
            cameraText.transform.SetParent(cameraSelect.transform, false);
            SetTextAndFont(cameraText, "Camera #?: No Camera Found");
            SetSizeDelta(cameraText, 615.01f, 65);
            var nextButton = new GameObject(nameof(CameraSelectUI.NextButton), typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CameraSelectUI.NextButton), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            nextButton.transform.SetParent(cameraSelect.transform, false);
            FixHorizontalLayoutGroup(nextButton);
            SetImage(nextButton, null);
            SetSizeDelta(nextButton, 86.17f, 65);
            var webcamDisplay = new GameObject(nameof(WebcamDisplay), typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(WebcamDisplay));
            webcamDisplay.transform.SetParent(cameraSelectionGroup.transform, false);
            SetSizeDelta(webcamDisplay, 435, 300);
            var webcamDisplayOutline = new GameObject("Outline", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            webcamDisplayOutline.transform.SetParent(webcamDisplay.transform, false);
            SetImage(webcamDisplayOutline, sprite);
            SetSizeDelta(webcamDisplayOutline, 10, 10);
            var faceVisible = new GameObject("FaceVisible", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Text), typeof(ContentSizeFitter));
            faceVisible.transform.SetParent(cameraSelectionGroup.transform, false);
            SetTextAndFont(faceVisible, "Please make sure your face is visible and well lit");
            SetSizeDelta(faceVisible, 862.51f, 105);
            var continueButton = new GameObject(nameof(Button), typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(ContentSizeFitter));
            continueButton.transform.SetParent(cameraSelectionGroup.transform, false);
            continueButton.GetComponent<Button>().onClick.AddListener(webcamDisplay.GetComponent<WebcamDisplay>().SetCameraIndex);
            SetImage(continueButton, null);
            SetSizeDelta(faceVisible, 240.01f, 65);
        }

        public static void SetSizeDelta(GameObject gameObject, float x, float y)
        {
            gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(x, y);
        }

        public static void SetImage(GameObject gameObject, Sprite sprite)
        {
            var image = gameObject.GetComponent<Image>();
            if (sprite != null) image.sprite = sprite;
            image.color = new Color(1f, 0.6784f, 0, 1);
        }

        public static void SetImage(GameObject gameObject, Color color)
        {
            var image = gameObject.GetComponent<Image>();
            image.color = color;
        }

        public static void SetTextAndFont(GameObject gameObject, string text, int size = 45)
        {
            var textUI = gameObject.GetComponent<Text>();
            textUI.color = new Color(1f, 0.6784f, 0, 1);
            textUI.text = text;
            textUI.font = Resources.FindObjectsOfTypeAll<Font>().FirstOrDefault(font => font.name == "Adobe - SerifGothicStd_Dynamic");
            textUI.fontSize = size;
            textUI.alignment = TextAnchor.LowerCenter;
            textUI.resizeTextForBestFit = true;
            textUI.resizeTextMaxSize = 72;
            textUI.resizeTextMinSize = 18;
            textUI.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        public static void FixHorizontalLayoutGroup(GameObject gameObject)
        {
            var horizontalLayoutGroup = gameObject.GetComponent<HorizontalLayoutGroup>();
            horizontalLayoutGroup.childScaleWidth = true;
            horizontalLayoutGroup.childScaleHeight = false;
            horizontalLayoutGroup.childForceExpandWidth = false;
            horizontalLayoutGroup.childForceExpandHeight = false;
            horizontalLayoutGroup.childControlWidth = false;
            horizontalLayoutGroup.childControlHeight = false;
            horizontalLayoutGroup.childAlignment = TextAnchor.MiddleCenter;
            horizontalLayoutGroup.spacing = 0;
        }

        public static void FixVerticalLayoutGroup(GameObject gameObject)
        {
            var verticalLayoutGroup = gameObject.GetComponent<VerticalLayoutGroup>();
            verticalLayoutGroup.childScaleWidth = false;
            verticalLayoutGroup.childScaleHeight = true;
            verticalLayoutGroup.childForceExpandWidth = false;
            verticalLayoutGroup.childForceExpandHeight = false;
            verticalLayoutGroup.childControlWidth = false;
            verticalLayoutGroup.childControlHeight = false;
            verticalLayoutGroup.childAlignment = TextAnchor.UpperCenter;
            verticalLayoutGroup.spacing = 30;
        }

        List<QuantumSocket[]> allQMSockets;
        public void SolarSystem()
        {
            CreatePipeReceiver();
            alarmStartTime = -1f;
            shipAudioController = GameObject.FindObjectOfType<ShipAudioController>();
            cameraEffectController = GameObject.FindObjectOfType<PlayerCameraEffectController>();
            _wakePrompt = new ScreenPrompt(InputLibrary.interact, UITextLibrary.GetString(UITextType.WakeUpPrompt), 0, ScreenPrompt.DisplayState.Normal, false);
            ResetRecollapse();
            PlanetPlayerWantsQMFor = -1;

            if (Locator.GetQuantumMoon() != null)
            {
                var qm = Locator.GetQuantumMoon().transform.Find("Sector_QuantumMoon");
                QuantumSocket[] GetSockets(string name)
                {
                    var state = qm.Find(name);
                    if (state == null) { ModHelper.Console.WriteLine($"QM {name} not found", MessageType.Error); return null; }
                    return state.GetComponentsInChildren<QuantumSocket>(true);
                }

                allQMSockets = new List<QuantumSocket[]>() {
                    GetSockets("State_HT"),
                    GetSockets("State_TH"),
                    GetSockets("State_BH"),
                    GetSockets("State_GD"),
                    GetSockets("State_DB"),
                    GetSockets("State_EYE"),
                };
            }
            else allQMSockets = new List<QuantumSocket[]>();
        }

        public const float closeEyesDuration = 0.05f;
        public const float openEyesDuration = 0.05f;
        public float blinkTimer;
        [Serializable]
        public enum BlinkState
        {
            Not,
            Blinking,
            WaitForRelease,
            Unblinking
        }
        public BlinkState state;
        [Serializable]
        public enum BlinkEffect
        {
            NormalBlinking,
            ManualBlinking
        }
        public BlinkEffect effect;

        public static bool QuantumObjectsShouldRecollapse { get; private set; }
        public bool waitForRecollapse;
        public bool updateState;
        public void ResetRecollapse()
        {
            updateState = false;
            waitForRecollapse = false;
            QuantumObjectsShouldRecollapse = false;
        }

        public float timeOfLastBlink;
        public static int PlanetPlayerWantsQMFor { get; private set; }
        public void CheckIfPlayerIsSeekingQuantumMoonchan()
        {
            PlanetPlayerWantsQMFor = -1;

            if ((PlayerState.IsInsideShip() || PlayerState.InZeroG()) && Locator.GetReferenceFrame() != null)
            {
                if (Time.timeSinceLevelLoad - timeOfLastBlink < 4f) // && Random.value > 0.75f)
                {
                    var orbit = Locator.GetReferenceFrame().GetOWRigidBody().GetComponentInChildren<QuantumOrbit>();
                    if (orbit != null)
                    {
                        PlanetPlayerWantsQMFor = orbit.GetStateIndex();
                    }
                }
            }
        }

        public bool IsSettingUp => PipeReceiver.blinkValue < -1f;
        public bool FaceFound => PipeReceiver.blinkValue > -1f;
        public bool NoFaceFound => PipeReceiver.blinkValue == -1f;

        public bool StartBlink()
        {
            switch (effect)
            {
                case BlinkEffect.NormalBlinking:
                    // Use PipeReceiver or other automatic blinking
                    return PipeReceiver.blinkValue > blinkingThreshhold;
                case BlinkEffect.ManualBlinking:
                    // Old keyboard/gamepad logic
                    return ManualBlinkStart();
                default:
                    return false;
            }
        }

        public bool ContinueBlink()
        {
            switch (effect)
            {
                case BlinkEffect.NormalBlinking:
                    return PipeReceiver.blinkValue > blinkingThreshhold;
                case BlinkEffect.ManualBlinking:
                    return ManualBlinkContinue();
                default:
                    return false;
            }
        }

        public bool ManualBlinkStart()
        {
            if (PlayerState.UsingShipComputer()) return false;

            if (Keyboard.current[Key.B].wasPressedThisFrame) return true; //Allow keyboard with gamepad
            if (OWInput.UsingGamepad())
            {
                if (PlayerState.InZeroG()) return false;
                if (Locator.GetToolModeSwapper().GetToolMode() != ToolMode.None || OWInput.IsInputMode(InputMode.ShipCockpit))
                {
                    return false;
                }
                return OWInput.IsNewlyPressed(InputLibrary.rollMode);
                //return Gamepad.current.leftShoulder.wasPressedThisFrame;
            }
            return false;
        }

        public bool ManualBlinkContinue()
        {
            if (PlayerState.UsingShipComputer()) return false;

            if (Keyboard.current[Key.B].isPressed) return true; //Allow keyboard with gamepad
            if (OWInput.UsingGamepad())
            {
                if (PlayerState.InZeroG()) return false;
                if (Locator.GetToolModeSwapper().GetToolMode() != ToolMode.None || OWInput.IsInputMode(InputMode.ShipCockpit))
                {
                    return false;
                }
                return OWInput.IsPressed(InputLibrary.rollMode);
                //return Gamepad.current.leftShoulder.isPressed;
            }
            return false;
        }

        public void Update()
        {
            if (alarmStartTime > 0f)
            {
                if (Time.timeSinceLevelLoad - alarmStartTime > 2f)
                {
                    if (shipAudioController != null) shipAudioController.StopAlarm();
                    alarmStartTime = -1f;
                }
            }

            switch (state)
            {
                case BlinkState.Not:

                    if (StartBlink() || storeBlinkInput)
                    {
                        if (PlayerState.OnQuantumMoon() && PlayerState.IsInsideShip()) return;

                        storeBlinkInput = false;
                        blinkTimer = 0f;
                        ResetRecollapse();
                        state = BlinkState.Blinking;
                        cameraEffectController.CloseEyes(closeEyesDuration);
                    }
                    break;
                case BlinkState.Blinking:

                    blinkTimer += Time.deltaTime;
                    if (blinkTimer > closeEyesDuration)
                    {
                        //GlobalMessenger.FireEvent("PlayerBlink"); //Want to re-collapse all quantum objects.
                        QuantumObjectsShouldRecollapse = true;

                        if (waitForRecollapse)
                        {
                            ModHelper.Console.WriteLine("Blink");
                            CheckIfPlayerIsSeekingQuantumMoonchan();
                            timeOfLastBlink = Time.timeSinceLevelLoad;
                            ResetRecollapse();
                            state = BlinkState.WaitForRelease;
                        }
                        waitForRecollapse = true;
                    }
                    break;
                case BlinkState.WaitForRelease:

                    if (updateState)
                    {
                        TeleportPlayerToSafeQMPosition();
                        FixQuantumState();
                    }
                    if (!updateState)
                    {
                        updateState = true;
                        return;
                    }

                    blinkTimer += Time.deltaTime;
                    if (!ContinueBlink())
                    {
                        blinkTimer = 0f;
                        state = BlinkState.Unblinking;
                        cameraEffectController.OpenEyes(openEyesDuration);
                    }
                    break;
                case BlinkState.Unblinking:
                    blinkTimer += Time.deltaTime;
                    if (blinkTimer > openEyesDuration)
                    {
                        state = BlinkState.Not;
                    }
                    if (StartBlink()) storeBlinkInput = true;
                    break;
            }
        }


        //--------------------------------------------- Quantum Moon Fixes ---------------------------------------------//
        QuantumSocket closestQMSocket;
        private Texture2D texture;
        private Sprite sprite;

        public void TeleportPlayerToSafeQMPosition()
        {
            QuantumMoon qm = Locator.GetQuantumMoon();
            closestQMSocket = null;

            if (qm == null) return;
            if (!qm.IsPlayerInside()) return;  //Behave normally outside and in shrines.
            if (qm.IsPlayerInsideShrine()) return;
            if (PlayerState.IsInsideShuttle()) return; //Make player teleport with shuttle

            var qmTF = qm.transform;

            /*
            //Find if player can exist here, go to socket if can't?
            bool groundBelow = false;
            var pos = Locator.GetPlayerBody().GetPosition();
            RaycastHit hitInfo;
            if (RaycastToQM(qmTF, pos, out hitInfo))
            {
                groundBelow = true;
            }
            */

            var pos = Locator.GetPlayerTransform().position;
            if (qm._stateIndex == QuantumMoon.EYE_INDEX) //Other Eye shrine sockets aren't any good.
            {
                var northPole = qm._shrine._northSocket;
                SetPlayerPos(northPole, qm);
                return;
            }

            var sockets = qm._shrine._socketList;
            float closestDist = float.MaxValue;
            foreach (var socket in sockets)
            {
                float dist = (pos - socket.transform.position).sqrMagnitude;
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestQMSocket = socket;
                }
            }
            //ModHelper.Console.WriteLine($"Dist: {closestDist}", MessageType.Debug);

            if (closestDist > 8f) //Allow warping to any socket if not too close to shrine/shuttle socket.
            {
                QuantumSocket[] currentSockets = allQMSockets[qm.GetStateIndex()];
                //ModHelper.Console.WriteLine($"Count for state {qm.GetStateIndex()}: {currentSockets.Length}", MessageType.Debug);

                var oldClosestQMSocket = closestQMSocket;
                float oldClosestDist = closestDist;
                closestDist = float.MaxValue;
                foreach (var socket in currentSockets)
                {
                    if (socket.IsOccupied() || !socket.IsActive()) continue;

                    float dist = (pos - socket.transform.position).sqrMagnitude;
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closestQMSocket = socket;
                    }
                }
                if (closestDist > oldClosestDist) closestQMSocket = oldClosestQMSocket; //Check if actually closer
            }

            if (closestQMSocket == null) return;
            SetPlayerPos(closestQMSocket, qm);
        }

        public void SetPlayerPos(QuantumSocket socket, QuantumMoon qm)
        {
            //Get Position
            var pos = socket.transform.position;
            var qmTF = qm.transform;
            var qmPos = qmTF.position;
            var upVec = (pos - qmPos);
            var upDir = upVec.normalized;
            var raycastOrigin = pos + upDir * 25f;
            if (Physics.Raycast(raycastOrigin, -upDir, out RaycastHit hitInfo, upVec.magnitude, OWLayerMask.groundMask))
            {
                pos = hitInfo.point;    //Fix instances of sockets being in the ground.
            }
            else
            {
                ModHelper.Console.WriteLine("Failed to find ground point", MessageType.Error);
            }
            pos += upDir;

            //Get Velocity
            var qmRB = qm.GetAttachedOWRigidbody();
            var playerPos = Locator.GetPlayerTransform().position;
            Vector3 velocityOffset = Locator.GetPlayerBody().GetVelocity() - qmRB.GetPointVelocity(playerPos);

            //Set Player
            Locator.GetPlayerBody().SetPosition(pos);
            Locator.GetPlayerBody().SetRotation(Quaternion.LookRotation(Locator.GetPlayerTransform().forward, upDir));
            Locator.GetPlayerBody().SetVelocity(qmRB.GetPointVelocity(socket.transform.position) + velocityOffset);
            GlobalMessenger.FireEvent("WarpPlayer"); //Fix shuttle volumes being stupid.
        }

        public void FixQuantumState()
        {
            var qm = Locator.GetQuantumMoon();
            if (qm == null) return;
            if (!qm.IsPlayerInside()) return;
            if (PlayerState.IsInsideShuttle()) return;
            if (closestQMSocket == null) return;

            var shrine = qm._shrine;

            /*
            var shuttle = qm.GetComponentInChildren<NomaiShuttleController>(); //Spawning on top now, so not needed.
            if (shuttle != null)
            {
                if (Vector3.Distance(shuttle.transform.position, closestQMSocket.transform.position) < 15f)
                {
                    //Causes issues if blink while tractor beaming
                    shuttle._tractorBeam.SetActivation(false, false);
                }
            }
            */

            //ModHelper.Console.WriteLine($"{shrine.GetCurrentSocket()} | {closestQMSocket}", MessageType.Debug);

            shrine._fading = true;  //Is this ever turned back to false?

            if (shrine.GetCurrentSocket() == closestQMSocket)
            {
                ModHelper.Console.WriteLine("Spawned inside shrine");
                shrine._triggerVolume.AddObjectToVolume(Locator.GetPlayerDetector()); //Make player exit properly
                shrine._isPlayerInside = true;
                shrine._exteriorLightController.FadeTo(0f, 0f);

                shrine._fadeFraction = shrine._isPlayerInside ? (shrine._gate.GetOpenFraction() * 0.7f) : 1f;
                shrine._ambientLight.intensity = shrine._origAmbientIntensity * shrine._fadeFraction;
                shrine._fogOverride.tint = Color.Lerp(Color.black, shrine._origFogTint, shrine._fadeFraction);
            }

            closestQMSocket = null;
        }
    }

    [HarmonyPatch]
    public static class Patches
    {
        //--------------------------------------------- Blinking/Quantum Moon Fixes ---------------------------------------------//
        [HarmonyPostfix]
        [HarmonyPatch(typeof(QuantumObject), nameof(QuantumObject.Update))]
        public static void QuantumObject_Update(QuantumObject __instance)
        {
            if (WebCameraBlink.QuantumObjectsShouldRecollapse)
            {
                if (Locator.GetQuantumMoon() != null && Locator.GetQuantumMoon().IsPlayerInsideShrine())
                {
                    if (__instance is QuantumShrine) return; //Keep player inside properly
                }

                __instance.Collapse(true);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(QuantumMoon), nameof(QuantumMoon.GetRandomStateIndex))]
        public static void GetRandomStateIndex(QuantumMoon __instance, ref int __result)
        {
            if (WebCameraBlink.PlanetPlayerWantsQMFor != -1)
            {
                WebCameraBlink.Instance.ModHelper.Console.WriteLine($"Forced moon for player {WebCameraBlink.QuantumObjectsShouldRecollapse}");
                __result = WebCameraBlink.PlanetPlayerWantsQMFor;
            }

            if (!__instance.IsPlayerInside()) return;  //Otherwise behave normally outside and in shrines.
            if (__instance.IsPlayerInsideShrine()) return;

            if (__result == QuantumMoon.EYE_INDEX)
            {
                var dir = (Locator.GetPlayerBody().GetPosition() - __instance.transform.position).normalized;
                float dot = Vector3.Dot(dir, __instance.transform.up);
                if (dot < 0.955f) //Re-roll if not at north pole
                {
                    __result = UnityEngine.Random.Range(0, 5);
                }
            }
        }
    }
}
