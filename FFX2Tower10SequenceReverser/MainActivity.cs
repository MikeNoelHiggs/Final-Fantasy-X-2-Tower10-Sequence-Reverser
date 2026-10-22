using Android.Content.PM;
using Android.Views;
using Android.Widget;

namespace FFX2Tower10SequenceReverser
{
    [Activity(
        Label = "@string/app_name",
        MainLauncher = true,
        ScreenOrientation = ScreenOrientation.Landscape,
        ConfigurationChanges = ConfigChanges.Orientation 
                            | ConfigChanges.ScreenSize
                            | ConfigChanges.KeyboardHidden
                            | ConfigChanges.Keyboard)]

    public class MainActivity : Activity
    {
        private int _currentTurn = 1;
        private int _inputCount = 0;
        private int _targetPlayerTwoId = -1;

        private bool _l2Down   = false;
        private bool _r2Down   = false;
        private bool _hatUp    = false;
        private bool _hatDown  = false;
        private bool _hatLeft  = false;
        private bool _hatRight = false;

        private ImageView[] _slots = null!;
        private LinearLayout _slotContainer = null!;
        private TextView _tvTurn = null!;
        private TextView _tvInputs = null!;
        private TextView _tvNoController = null!;
        private int _slotSize = 0;   


        private static readonly Dictionary<Keycode, int> ButtonDrawables = new()
        {
            { Keycode.ButtonA,   Resource.Drawable.cross_button    },
            { Keycode.ButtonB,   Resource.Drawable.circle_button   },
            { Keycode.ButtonX,   Resource.Drawable.square_button   },
            { Keycode.ButtonY,   Resource.Drawable.triangle_button },
            { Keycode.ButtonL1,  Resource.Drawable.l1_button       },
            { Keycode.ButtonR1,  Resource.Drawable.r1_button       },
            { Keycode.DpadUp,    Resource.Drawable.up_button       },
            { Keycode.DpadDown,  Resource.Drawable.down_button     },
            { Keycode.DpadLeft,  Resource.Drawable.left_button     },
            { Keycode.DpadRight, Resource.Drawable.right_button    },
        };


        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);
            ActionBar?.Hide();
            HideSystemBars();  

            _slots = new[]
            {
                FindViewById<ImageView>(Resource.Id.slot_0)!,
                FindViewById<ImageView>(Resource.Id.slot_1)!,
                FindViewById<ImageView>(Resource.Id.slot_2)!,
                FindViewById<ImageView>(Resource.Id.slot_3)!,
                FindViewById<ImageView>(Resource.Id.slot_4)!,
                FindViewById<ImageView>(Resource.Id.slot_5)!,
            };

            _tvTurn         = FindViewById<TextView>(Resource.Id.tv_turn)!;
            _tvInputs        = FindViewById<TextView>(Resource.Id.tv_inputs)!;
            _tvNoController  = FindViewById<TextView>(Resource.Id.tv_no_controller)!;
            _slotContainer   = FindViewById<LinearLayout>(Resource.Id.slot_container)!;

            DetectControllers(); 
            ApplyTurnState();     
        }

        /// <summary>
        /// Re-checks for a connected controller each time the activity becomes visible,
        /// so the warning clears automatically once the user pairs their controller.
        /// </summary>
        protected override void OnResume()
        {
            base.OnResume();
            DetectControllers();
        }

        /// <summary>
        /// Fires when the window gains or loses focus.
        /// Re-applies immersive mode each time focus is regained (system bars can
        /// temporarily reappear after overlays) and computes slot size on first focus.
        /// </summary>
        public override void OnWindowFocusChanged(bool hasFocus)
        {
            base.OnWindowFocusChanged(hasFocus);
            if (!hasFocus) return;

            HideSystemBars();

            if (_slotSize == 0)
                ComputeAndApplySlotSize();
        }

        /// <summary>
        /// Hides the status bar and navigation bar using Immersive Sticky mode.
        /// The bars reappear temporarily only when the user swipes from the screen edge.
        /// </summary>
        private void HideSystemBars()
        {
            var ic = Window?.InsetsController;
            if (ic is null) return;

            ic.Hide(WindowInsets.Type.SystemBars());
            ic.SystemBarsBehavior =
                (int)WindowInsetsControllerBehavior.ShowTransientBarsBySwipe;
        }

        /// <summary>
        /// Derives <see cref="_slotSize"/> from the slot container’s measured width
        /// divided by 6, minus per-slot margins. Applies it as a square to every slot.
        /// </summary>
        private void ComputeAndApplySlotSize()
        {
            int available = _slotContainer.Width
                          - _slotContainer.PaddingLeft
                          - _slotContainer.PaddingRight;

            int marginPx = (int)(4 * Resources!.DisplayMetrics!.Density + 0.5f);
            _slotSize = (available - 6 * marginPx * 2) / 6;

            if (_slotSize <= 0) return;

            foreach (var slot in _slots)
            {
                var lp = (LinearLayout.LayoutParams)slot.LayoutParameters!;
                lp.Width  = _slotSize;
                lp.Height = _slotSize;
                lp.Weight = 0;   // fixed size, no longer distributed by weight
                slot.LayoutParameters = lp;
            }
        }

        /// <summary>
        /// Iterates all connected input devices and assigns the first gamepad/joystick
        /// found to <see cref="_targetPlayerTwoId"/>.
        /// Shows a warning if no controller is detected; hides it when one is found.
        /// Call again if controllers are hot-swapped.
        /// </summary>
        private void DetectControllers()
        {
            _targetPlayerTwoId = -1;

            foreach (int id in InputDevice.GetDeviceIds())
            {
                var device = InputDevice.GetDevice(id);
                if (device is null) continue;

                int src = (int)device.Sources;
                bool isGamepad =
                    (src & (int)InputSourceType.Gamepad) == (int)InputSourceType.Gamepad ||
                    (src & (int)InputSourceType.Joystick) == (int)InputSourceType.Joystick;

                if (!isGamepad) continue;

                _targetPlayerTwoId = id;
                break;
            }

            _tvNoController.Visibility =
                _targetPlayerTwoId == -1 ? ViewStates.Visible : ViewStates.Gone;
        }

        /// <summary>
        /// Intercepts all key events at window level — before any view in the layout
        /// can consume them. This is required for L1/R1 which Android’s view-focus
        /// system can absorb before they reach Activity.OnKeyDown.
        /// Only ACTION_DOWN with RepeatCount 0 is handled; everything else passes through.
        /// </summary>
        public override bool DispatchKeyEvent(KeyEvent? e)
        {
            if (e is null || e.Action != KeyEventActions.Down || e.RepeatCount > 0)
                return base.DispatchKeyEvent(e);

            switch (e.KeyCode)
            {
                case Keycode.ButtonStart:  
                    AdvanceTurn();
                    return true;

                case Keycode.ButtonSelect: 
                    ResetAll();
                    return true;

                default:
                    if (ButtonDrawables.TryGetValue(e.KeyCode, out int drawableId))
                    {
                        if (e.KeyCode is Keycode.DpadUp or Keycode.DpadDown
                                      or Keycode.DpadLeft or Keycode.DpadRight)
                        {
                            bool fromRealDpad =
                                ((int)e.Source & (int)InputSourceType.Dpad) ==
                                (int)InputSourceType.Dpad;

                            if (!fromRealDpad)
                                return base.DispatchKeyEvent(e);
                        }

                        RecordInput(drawableId);
                        return true;
                    }
                    break;
            }

            return base.DispatchKeyEvent(e);
        }

        /// <summary>
        /// Handles analog inputs that arrive as MotionEvents rather than key events.
        /// PS5 DualSense via Bluetooth sends L2/R2 as Axis.Brake/Gas and the
        /// D-pad as Axis.HatX/HatY. State fields debounce each axis so only the
        /// pressed edge fires RecordInput, not every intermediate value.
        /// </summary>
        public override bool OnGenericMotionEvent(MotionEvent? e)
        {
            if (e is null) return base.OnGenericMotionEvent(e);

            int src = (int)e.Source;
            bool isJoystick =
                (src & (int)InputSourceType.Joystick) == (int)InputSourceType.Joystick;

            if (!isJoystick) return base.OnGenericMotionEvent(e);

            bool l2Now = e.GetAxisValue(Axis.Brake) > 0.5f;
            bool r2Now = e.GetAxisValue(Axis.Gas)   > 0.5f;

            if (l2Now && !_l2Down) RecordInput(Resource.Drawable.l2_button);
            if (r2Now && !_r2Down) RecordInput(Resource.Drawable.r2_button);

            _l2Down = l2Now;
            _r2Down = r2Now;

            bool hatUpNow    = e.GetAxisValue(Axis.HatY) < -0.5f;
            bool hatDownNow  = e.GetAxisValue(Axis.HatY) >  0.5f;
            bool hatLeftNow  = e.GetAxisValue(Axis.HatX) < -0.5f;
            bool hatRightNow = e.GetAxisValue(Axis.HatX) >  0.5f;

            if (hatUpNow    && !_hatUp)    RecordInput(Resource.Drawable.up_button);
            if (hatDownNow  && !_hatDown)  RecordInput(Resource.Drawable.down_button);
            if (hatLeftNow  && !_hatLeft)  RecordInput(Resource.Drawable.left_button);
            if (hatRightNow && !_hatRight) RecordInput(Resource.Drawable.right_button);

            _hatUp    = hatUpNow;
            _hatDown  = hatDownNow;
            _hatLeft  = hatLeftNow;
            _hatRight = hatRightNow;

            return base.OnGenericMotionEvent(e);
        }

        /// <summary>
        /// Returns the number of required inputs (N) for the given turn.
        ///   1–11 → 2    12–16 → 3    17–21 → 4    22–26 → 5    27–30 → 6
        /// </summary>
        private static int GetN(int turn) => turn switch
        {
            <= 11 => 2,   //  1–11
            <= 16 => 3,   // 12–16
            <= 21 => 4,   // 17–21
            <= 26 => 5,   // 22–26
            _     => 6,   // 27–30
        };

        /// <summary>
        /// Records one button press into the correct slot, filling right-to-left
        /// within the left-aligned visible window.
        /// Slot index formula: (N - 1) - i   where i = current _inputCount.
        /// e.g. N=3: press 0 → slot 2, press 1 → slot 1, press 2 → slot 0.
        /// Reading the completed row left→right gives the reversed sequence.
        /// </summary>
        private void RecordInput(int drawableId)
        {
            int n = GetN(_currentTurn);
            if (_inputCount >= n) return;   // turn is full — wait for Options press

            int slotIndex = (n - 1) - _inputCount;
            _slots[slotIndex].SetImageResource(drawableId);

            _inputCount++;
            UpdateHud();
        }

        /// <summary>
        /// Increments <see cref="_currentTurn"/> (capped at 30), resets the input
        /// pointer, and refreshes slot visibility and the HUD.
        /// Does nothing when already on turn 30 (display: 30/30).
        /// </summary>
        private void AdvanceTurn()
        {
            if (_currentTurn >= 30) return;   // 30/30 reached — Options does nothing
            _currentTurn++;
            _inputCount = 0;
            ApplyTurnState();
        }

        /// <summary>
        /// Resets <see cref="_currentTurn"/> to 1 and <see cref="_inputCount"/> to 0,
        /// then restores the UI to its initial state.
        /// </summary>
        private void ResetAll()
        {
            _currentTurn = 1;
            _inputCount = 0;
            ApplyTurnState();
        }

        /// <summary>
        /// Sets correct Gone/Visible state and clears images for every slot,
        /// then refreshes the HUD. Called on every turn boundary.
        /// </summary>
        private void ApplyTurnState()
        {
            int n = GetN(_currentTurn);

            for (int i = 0; i < 6; i++)
            {
                if (i < n)
                {
                    _slots[i].Visibility = ViewStates.Visible;
                    _slots[i].SetImageDrawable(null);
                }
                else
                {
                    _slots[i].Visibility = ViewStates.Gone;
                }
            }

            UpdateHud();
        }

        /// <summary>
        /// Refreshes the turn label and input-progress label in the HUD.
        /// </summary>
        private void UpdateHud()
        {
            int n = GetN(_currentTurn);
            _tvTurn.Text = $"Turn {_currentTurn}/30";
            _tvInputs.Text = $"Inputs: {_inputCount}/{n}";
        }
    }
}