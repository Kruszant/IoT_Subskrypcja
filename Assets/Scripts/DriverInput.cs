using UnityEngine;
#if ENABLE_INPUT_SYSTEM && IOT_INPUT_SYSTEM_PACKAGE && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace IoT.Subskrypcja
{
    /// <summary>
    /// Odczyt klawiatury niezależny od ustawienia "Active Input Handling":
    /// - "Input Manager (Old)" albo "Both": klasyczne Input.GetAxis,
    /// - tylko nowy Input System: Keyboard.current.
    /// Wybór odbywa się w czasie kompilacji (symbole ENABLE_LEGACY_INPUT_MANAGER / ENABLE_INPUT_SYSTEM).
    /// </summary>
    public static class DriverInput
    {
        /// <summary>W = +1, S = -1.</summary>
        public static float Gaz()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetAxis("Vertical");
#elif ENABLE_INPUT_SYSTEM && IOT_INPUT_SYSTEM_PACKAGE
            Keyboard k = Keyboard.current;
            if (k == null) return 0f;
            float v = 0f;
            if (k.wKey.isPressed || k.upArrowKey.isPressed) v += 1f;
            if (k.sKey.isPressed || k.downArrowKey.isPressed) v -= 1f;
            return v;
#else
            return 0f;
#endif
        }

        /// <summary>D = +1, A = -1.</summary>
        public static float Skret()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetAxis("Horizontal");
#elif ENABLE_INPUT_SYSTEM && IOT_INPUT_SYSTEM_PACKAGE
            Keyboard k = Keyboard.current;
            if (k == null) return 0f;
            float v = 0f;
            if (k.dKey.isPressed || k.rightArrowKey.isPressed) v += 1f;
            if (k.aKey.isPressed || k.leftArrowKey.isPressed) v -= 1f;
            return v;
#else
            return 0f;
#endif
        }

        /// <summary>B = otwórz / zamknij sklep subskrypcji (tylko w klatce wciśnięcia).</summary>
        public static bool KlawiszSklepu()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.B);
#elif ENABLE_INPUT_SYSTEM && IOT_INPUT_SYSTEM_PACKAGE
            Keyboard k = Keyboard.current;
            return k != null && k.bKey.wasPressedThisFrame;
#else
            return false;
#endif
        }

        /// <summary>1 = pierwsza pozycja sklepu (kamera cofania).</summary>
        public static bool KlawiszOpcji1()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1);
#elif ENABLE_INPUT_SYSTEM && IOT_INPUT_SYSTEM_PACKAGE
            Keyboard k = Keyboard.current;
            return k != null && (k.digit1Key.wasPressedThisFrame || k.numpad1Key.wasPressedThisFrame);
#else
            return false;
#endif
        }

        /// <summary>2 = druga pozycja sklepu (czujnik parkowania).</summary>
        public static bool KlawiszOpcji2()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2);
#elif ENABLE_INPUT_SYSTEM && IOT_INPUT_SYSTEM_PACKAGE
            Keyboard k = Keyboard.current;
            return k != null && (k.digit2Key.wasPressedThisFrame || k.numpad2Key.wasPressedThisFrame);
#else
            return false;
#endif
        }

        /// <summary>Spacja = hamulec ręczny.</summary>
        public static bool HamulecReczny()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKey(KeyCode.Space);
#elif ENABLE_INPUT_SYSTEM && IOT_INPUT_SYSTEM_PACKAGE
            Keyboard k = Keyboard.current;
            return k != null && k.spaceKey.isPressed;
#else
            return false;
#endif
        }
    }
}
