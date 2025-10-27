using UnityEngine;
using UnityEngine.InputSystem;

namespace FumoCore.Input
{
    [DefaultExecutionOrder(-100)]
    public class ShmupInput : MonoBehaviour
    {
        [System.Serializable]
        private class Map
        {
            public InputActionReference moveAction;
            public InputActionReference shootAction;
            public InputActionReference focusAction;
            public InputActionReference bombAction;
            public InputActionReference skipDialogueAction;
            public InputActionReference reloadPracticeWarp;
            public InputActionReference chargeAction;
            public void Enable()
            {
                moveAction.action.Enable();
                shootAction.action.Enable();
                focusAction.action.Enable();
                bombAction.action.Enable();
                skipDialogueAction.action.Enable();
                reloadPracticeWarp.action.Enable();
                chargeAction.action.Enable();
            }
            public void Disable()
            {
                moveAction.action.Disable();
                shootAction.action.Disable();
                focusAction.action.Disable();
                bombAction.action.Disable();
                skipDialogueAction.action.Disable();
                reloadPracticeWarp.action.Disable();
                chargeAction.action.Disable();
            }
        }
        [SerializeField] private Map inputMap;
        private static ShmupInput instance;

        private InputAction moveAction;
        private InputAction shootAction;
        private InputAction focusAction;
        private InputAction bombAction;
        private InputAction skipDialogueAction;
        private InputAction reloadPracticeAction;
        private InputAction chargeAction;

        private class ButtonStateTracker
        {
            public bool IsPressed { get; private set; }
            public bool JustPressed { get; private set; }
            public float PressStartTime { get; private set; } = -1f;
            public float ReleaseTime { get; private set; } = -1f;
            public void Update(bool currentlyPressed)
            {
                JustPressed = currentlyPressed && !IsPressed;

                if (JustPressed)
                    PressStartTime = Time.unscaledTime;

                if (!currentlyPressed && IsPressed)
                    ReleaseTime = Time.unscaledTime;

                if (!currentlyPressed)
                    PressStartTime = -1f;

                IsPressed = currentlyPressed;
            }
            public bool PressedLongerThan(float duration)
            {
                return IsPressed && PressStartTime >= 0f && (Time.unscaledTime - PressStartTime) >= duration;
            }
            public bool ReleasedLongerThan(float duration)
            {
                return !IsPressed && (ReleaseTime < 0f || (Time.unscaledTime - ReleaseTime) >= duration);
            }
        }
        private ButtonStateTracker shootTracker = new ButtonStateTracker();
        private ButtonStateTracker focusTracker = new ButtonStateTracker();
        private ButtonStateTracker bombTracker = new ButtonStateTracker();
        private ButtonStateTracker skipDialogueTracker = new ButtonStateTracker();
        private ButtonStateTracker reloadPracticeTracker = new ButtonStateTracker();
        private ButtonStateTracker chargeTracker = new ButtonStateTracker();

        private void Awake()
        {
            instance = this;

            moveAction = inputMap.moveAction.action;
            shootAction = inputMap.shootAction.action;
            focusAction = inputMap.focusAction.action;
            bombAction = inputMap.bombAction.action;
            skipDialogueAction = inputMap.skipDialogueAction.action;
            reloadPracticeAction = inputMap.reloadPracticeWarp.action;
            chargeAction = inputMap.chargeAction.action;
        }
        private void Start()
        {
            inputMap.Enable();
        }
        private void OnDestroy()
        {
            inputMap.Disable();
        }
        private void Update()
        {
            shootTracker.Update(shootAction.IsPressed());
            focusTracker.Update(focusAction.IsPressed());
            bombTracker.Update(bombAction.IsPressed());
            skipDialogueTracker.Update(skipDialogueAction.IsPressed());
            reloadPracticeTracker.Update(reloadPracticeAction.IsPressed());
            chargeTracker.Update(chargeAction.IsPressed());
        }
        public static Vector2 Move => instance == null ? Vector2.zero : instance.moveAction.ReadValue<Vector2>();
        public static bool Shoot => instance?.shootTracker.IsPressed ?? false;
        public static bool ShootJustPressed => instance?.shootTracker.JustPressed ?? false;
        public static bool ShootPressedLongerThan(float seconds) => instance?.shootTracker.PressedLongerThan(seconds) ?? false;
        public static bool ShootReleasedLongerThan(float seconds) => instance?.shootTracker.ReleasedLongerThan(seconds) ?? false;
        public static bool Focus => instance?.focusTracker.IsPressed ?? false;
        public static bool FocusJustPressed => instance?.focusTracker.JustPressed ?? false;
        public static bool FocusPressedLongerThan(float seconds) => instance?.focusTracker.PressedLongerThan(seconds) ?? false;
        public static bool FocusReleasedLongerThan(float seconds) => instance?.focusTracker.ReleasedLongerThan(seconds) ?? false;
        public static bool Bomb => instance?.bombTracker.IsPressed ?? false;
        public static bool BombJustPressed => instance?.bombTracker.JustPressed ?? false;
        public static bool BombPressedLongerThan(float seconds) => instance?.bombTracker.PressedLongerThan(seconds) ?? false;
        public static bool BombReleasedLongerThan(float seconds) => instance?.bombTracker.ReleasedLongerThan(seconds) ?? false;
        public static bool SkipDialogue => instance?.skipDialogueTracker.IsPressed ?? false;
        public static bool SkipDialogueJustPressed => instance?.skipDialogueTracker.JustPressed ?? false;
        public static bool SkipDialoguePressedLongerThan(float seconds) => instance?.skipDialogueTracker.PressedLongerThan(seconds) ?? false;
        public static bool SkipDialogueReleasedLongerThan(float seconds) => instance?.skipDialogueTracker.ReleasedLongerThan(seconds) ?? false;
        public static bool ReloadPractice => instance?.reloadPracticeTracker.IsPressed ?? false;
        public static bool ReloadPracticeJustPressed => instance?.reloadPracticeTracker.JustPressed ?? false;
        public static bool ReloadPracticePressedLongerThan(float seconds) => instance?.reloadPracticeTracker.PressedLongerThan(seconds) ?? false;
        public static bool ReloadPracticeReleasedLongerThan(float seconds) => instance?.reloadPracticeTracker.ReleasedLongerThan(seconds) ?? false;
        public static bool Charging => instance?.chargeTracker.IsPressed ?? false;
        public static bool ChargeJustPressed => instance?.chargeTracker.JustPressed ?? false;
        public static bool ChargePressedLongerThan(float seconds) => instance?.chargeTracker.PressedLongerThan(seconds) ?? false;
        public static bool ChargeReleasedLongerThan(float seconds) => instance?.chargeTracker.ReleasedLongerThan(seconds) ?? false;
    }
}
