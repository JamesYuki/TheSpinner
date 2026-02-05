using PurrNet.Prediction.StateMachine;
using UnityEngine;

public interface IStateUI
{
    void Activate(IPredictedStateNodeBase predictedStateNode, object param = null);
    void UpdateUI(IPredictedStateNodeBase predictedStateNode, object param = null);
}

[RequireComponent(typeof(CanvasGroup))]
public abstract class InGameStateUIBase<TState> : InGameUIBase, IStateUI where TState : IPredictedStateNodeBase
{
    public void Activate(IPredictedStateNodeBase predictedStateNode, object param = null)
    {
        if (predictedStateNode is TState)
        {
            Show();
            OnActivate((TState)predictedStateNode, param);
        }
        else
        {
            Hide();
        }
    }

    protected virtual void OnActivate(TState state, object param = null) { }

    public void UpdateUI(IPredictedStateNodeBase predictedStateNode, object param = null)
    {
        if (!(predictedStateNode is TState state))
        {
            return;
        }

        OnUpdateUI(state, param);
    }

    protected virtual void OnUpdateUI(TState state, object param = null) { }
}
