using UnityEngine;
using System.Collections;
using PlayTable;

public enum GameButtonState
{
    On,
    Off,
    SelectedEnd,
    SelectedContinue
}

public abstract class PTGameButton_new : PTLocalInput_new
{
    [SerializeField] private SpriteRenderer availableSprite;
    [SerializeField] private SpriteRenderer selectedSprite;
    public GameButtonState ButtonState { get; protected set; }

    protected virtual void Start() {
        ButtonState = GameButtonState.On;
        OnLongHoldBegin += Handler_OnLongHoldBegin;
        OnTouchBegin += Handler_OnTouchBegin;
        OnShortHoldBegin += Handler_OnShortHoldBegin;

        PTGlobalInput_new.OnTouchBegin += (PTTouch touch) => 
        {
            if (touch.hitsRealtime.Contains(GetComponent<Collider>())) 
            {
                touch.canPenetrate = false;
            }
        };
    }

    private void Handler_OnShortHoldBegin(PTTouch touch)
    {
        if (ButtonState == GameButtonState.Off ||
            ButtonState == GameButtonState.SelectedEnd)
        {
            return;
        }
        StartCoroutine(OnShortHoldClickCoroutine());
    }

    private void Handler_OnTouchBegin(PTTouch touch)
    {
        if (ButtonState == GameButtonState.Off ||
            ButtonState == GameButtonState.SelectedEnd)
        {
            return;
        }
        StartCoroutine(OnTouchBeginCoroutine());
    }

    private void Handler_OnLongHoldBegin(PTTouch touch)
    {
        if (ButtonState == GameButtonState.Off ||
            ButtonState == GameButtonState.SelectedEnd)
        {
            return;
        }
        StartCoroutine(OnLongHoldClickCoroutine());
    }

    protected abstract IEnumerator OnLongHoldClickCoroutine();

    protected abstract IEnumerator OnShortHoldClickCoroutine();

    protected abstract IEnumerator OnTouchBeginCoroutine();

    public virtual IEnumerator OnSetOnCoroutine(float timer)
    {
        yield return selectedSprite.SetAlphaCoroutine(0f, timer);
        yield return availableSprite.SetAlphaCoroutine(1f, timer);
        ButtonState = GameButtonState.On;
    }

    public virtual IEnumerator OnSetOffCoroutine(float timer)
    {
        ButtonState = GameButtonState.Off;
        yield return selectedSprite.SetAlphaCoroutine(0f, timer);
        yield return availableSprite.SetAlphaCoroutine(0f, timer);
    }

    public virtual IEnumerator OnSetSelectedEndCoroutine(float timer)
    {
        ButtonState = GameButtonState.SelectedEnd;
        yield return selectedSprite.SetAlphaCoroutine(1f, timer);
        yield return availableSprite.SetAlphaCoroutine(0f, timer);
    }

    public virtual IEnumerator OnSetSelectedContinueCoroutine(float timer)
    {
        ButtonState = GameButtonState.SelectedContinue;
        yield return selectedSprite.SetAlphaCoroutine(1f, timer);
    }
}