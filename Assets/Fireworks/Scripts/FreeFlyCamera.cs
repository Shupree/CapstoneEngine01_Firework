using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Capstone.Fireworks
{
    [DisallowMultipleComponent, RequireComponent(typeof(Camera))]
    public sealed class FreeFlyCamera : MonoBehaviour
    {
        [Min(1)] public float moveSpeed = 25;
        [Min(1)] public float fastMultiplier = 3;
        [Range(0.02f, 1)] public float lookSensitivity = 0.12f;
        public float minimumHeight = 2;
        public float maximumHeight = 350;
        private Vector3 homePosition;
        private Quaternion homeRotation;
        private bool looking;

        private void Awake() { homePosition = transform.position; homeRotation = transform.rotation; }
        private void Update()
        {
            Vector2 look = Vector2.zero;
            Vector3 move = Vector3.zero;
            bool right = false, fast = false, reset = false, escape = false;
            float scroll = 0;
#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            var keys = Keyboard.current;
            if (mouse != null)
            {
                right = mouse.rightButton.isPressed;
                look = mouse.delta.ReadValue();
                scroll = mouse.scroll.ReadValue().y;
            }
            if (keys != null)
            {
                move = new Vector3((keys.dKey.isPressed ? 1 : 0) - (keys.aKey.isPressed ? 1 : 0),
                    (keys.eKey.isPressed ? 1 : 0) - (keys.qKey.isPressed ? 1 : 0),
                    (keys.wKey.isPressed ? 1 : 0) - (keys.sKey.isPressed ? 1 : 0));
                fast = keys.leftShiftKey.isPressed || keys.rightShiftKey.isPressed;
                reset = keys.homeKey.wasPressedThisFrame;
                escape = keys.escapeKey.wasPressedThisFrame;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            right = Input.GetMouseButton(1);
            look = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 15;
            move = new Vector3(Input.GetAxisRaw("Horizontal"), (Input.GetKey(KeyCode.E)?1:0)-(Input.GetKey(KeyCode.Q)?1:0), Input.GetAxisRaw("Vertical"));
            fast = Input.GetKey(KeyCode.LeftShift);
            reset = Input.GetKeyDown(KeyCode.Home);
            escape = Input.GetKeyDown(KeyCode.Escape);
            scroll = Input.mouseScrollDelta.y * 120;
#endif
            if (reset) { transform.SetPositionAndRotation(homePosition, homeRotation); }
            if (escape || !Application.isFocused) { ReleaseCursor(); return; }
            if (right != looking)
            {
                looking = right;
                Cursor.lockState = right ? CursorLockMode.Locked : CursorLockMode.None;
                Cursor.visible = !right;
                look = Vector2.zero;
            }
            if (!looking) return;
            moveSpeed = Mathf.Clamp(moveSpeed * Mathf.Pow(1.15f, scroll / 120), 2, 150);
            ApplyMotion(move, look, fast, Time.unscaledDeltaTime);
        }

        public void ApplyMotion(Vector3 input, Vector2 lookDelta, bool fast, float deltaTime)
        {
            var angles = transform.eulerAngles;
            float pitch = Mathf.DeltaAngle(0, angles.x);
            pitch = Mathf.Clamp(pitch - lookDelta.y * lookSensitivity, -85, 85);
            transform.rotation = Quaternion.Euler(pitch, angles.y + lookDelta.x * lookSensitivity, 0);
            Vector3 direction = transform.right * input.x + Vector3.up * input.y + transform.forward * input.z;
            if (direction.sqrMagnitude > 1) direction.Normalize();
            Vector3 position = transform.position + direction * moveSpeed * (fast ? fastMultiplier : 1) * Mathf.Max(0, deltaTime);
            position.y = Mathf.Clamp(position.y, minimumHeight, maximumHeight);
            transform.position = position;
        }
        private void ReleaseCursor() { looking = false; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        private void OnDisable() { ReleaseCursor(); }
        private void OnApplicationFocus(bool focused) { if (!focused) ReleaseCursor(); }
    }
}
