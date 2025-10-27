using FumoCore.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Fumorin
{
    [RequireComponent(typeof(Button))]
    public class QuitButton : MonoBehaviour
    {
        Button b;
        private void Awake()
        {
            b = GetComponent<Button>();
        }
        private void Start()
        {
            b.AddClickAction(() => Application.Quit());
            b.interactable = !GeneralManager.IsWebGL;
        }
        private void OnDestroy()
        {
            b.RemoveClickAction(() => Application.Quit());
        }
    }
}
