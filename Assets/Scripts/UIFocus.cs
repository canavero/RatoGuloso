using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Deixa os menus navegáveis pelo controle/teclado: ao abrir o painel, seleciona o primeiro botão,
// e se nada estiver selecionado (ex.: depois de clicar fora com o mouse) seleciona de novo.
public class UIFocus : MonoBehaviour
{
    public Selectable firstSelected;

    void OnEnable()
    {
        Select();
    }

    // o EventSystem pode ainda não existir no OnEnable do primeiro frame
    void Start()
    {
        Select();
    }

    void Update()
    {
        EventSystem es = EventSystem.current;
        if (es == null || firstSelected == null) return;
        GameObject current = es.currentSelectedGameObject;
        if ((current == null || !current.activeInHierarchy) && GameInput.Move().sqrMagnitude > 0.25f) Select();
    }

    void Select()
    {
        if (firstSelected != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(firstSelected.gameObject);
    }
}
