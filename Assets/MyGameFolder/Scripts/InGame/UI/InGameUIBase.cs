using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class InGameUIBase : MonoBehaviour
{
    protected CanvasGroup m_CanvasGroup;

    protected virtual void Awake()
    {
        m_CanvasGroup = GetComponent<CanvasGroup>();
    }

    public virtual void Show()
    {
        m_CanvasGroup.alpha = 1.0f;
        m_CanvasGroup.interactable = true;
        m_CanvasGroup.blocksRaycasts = true;
    }

    public virtual void Hide()
    {
        m_CanvasGroup.alpha = 0.0f;
        m_CanvasGroup.interactable = false;
        m_CanvasGroup.blocksRaycasts = false;
    }
}
