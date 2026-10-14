using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

// Coloca no EventSystem o módulo de input certo para a UI (mouse, teclado e controle),
// de acordo com o sistema de input ativo no projeto (novo Input System ou Input Manager antigo).
[RequireComponent(typeof(EventSystem))]
public class UIInputSetup : MonoBehaviour
{
    void Awake()
    {
#if ENABLE_INPUT_SYSTEM
#if !ENABLE_LEGACY_INPUT_MANAGER
        // StandaloneInputModule usa o Input Manager antigo e daria erro a cada frame
        StandaloneInputModule old = GetComponent<StandaloneInputModule>();
        if (old != null)
        {
            old.enabled = false;
            Destroy(old);
        }
#endif
        InputSystemUIInputModule module = GetComponent<InputSystemUIInputModule>();
        if (module == null) module = gameObject.AddComponent<InputSystemUIInputModule>();
        if (module.actionsAsset == null) module.AssignDefaultActions();
#else
        if (GetComponent<BaseInputModule>() == null) gameObject.AddComponent<StandaloneInputModule>();
#endif
    }
}
