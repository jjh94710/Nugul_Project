using UnityEngine;
using UnityEngine.UI;

public class StartCanvas : MonoBehaviour
{   
    public Button startButton;

    private void Awake()
    {
        if (startButton == null)
        {
            startButton = GetComponentInChildren<Button>(true);
        }
    }

    private void OnEnable()
    {
        startButton.onClick.AddListener(Hide);
    }

    private void OnDisable()
    {
        startButton.onClick.RemoveListener(Hide);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
