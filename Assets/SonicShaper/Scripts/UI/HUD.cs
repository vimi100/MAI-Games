using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUD : MonoBehaviour
{
    [Header("References")]
    public SoundEmitter soundEmitter;

    [Header("UI Elements")]
    public Image frequencyIndicator;
    public Image cooldownBar;
    public TextMeshProUGUI modeLabel;

    [Header("Colors")]
    public Color readyColor = new Color(0.25f, 0.95f, 0.95f);
    public Color cooldownColor = new Color(1f, 0.45f, 0.2f);
    public Color grabColor = new Color(0.95f, 0.85f, 0.2f);

    void Update()
    {
        if (soundEmitter == null) return;

        bool isEchoAbility = soundEmitter.CurrentAbility == AbilityMode.EchoCopy;
        bool isReady = soundEmitter.IsReady;
        Color currentColor = isEchoAbility
            ? (isReady ? readyColor : cooldownColor)
            : grabColor;

        if (frequencyIndicator != null)
            frequencyIndicator.color = currentColor;

        if (cooldownBar != null)
        {
            cooldownBar.fillAmount = isEchoAbility ? (1f - soundEmitter.GetCooldownRatio()) : 1f;
            cooldownBar.color = currentColor;
        }

        if (modeLabel != null)
        {
            if (isEchoAbility)
            {
                if (isReady)
                    modeLabel.text = $"ABILITY: ECHO COPY\nREADY {soundEmitter.ActiveCopyCount}/{soundEmitter.MaxCopyCount}";
                else
                    modeLabel.text = $"ABILITY: ECHO COPY\nCD {soundEmitter.CooldownRemaining:0.0}s";
            }
            else
            {
                modeLabel.text = soundEmitter.IsHoldingObject
                    ? $"ABILITY: GRAB\nHOLDING: {soundEmitter.HeldObjectName}"
                    : "ABILITY: GRAB\nREADY (E to pick/drop)";
            }
        }
    }
}
