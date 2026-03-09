using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SeekBehaviour : MonoBehaviour
{
    [SerializeField] RectTransform seekBarTimePreviewTransform;
    [SerializeField] TMP_Text seekBarTimePreviewText;
    [SerializeField] CanvasScaler currentCanvasScaler;
    [SerializeField] ReplayViewerScript replayViewerScript;

    bool mouseOverSeek;
    float referenceRes;
    bool scrubbing;

    void Awake()
    {
        referenceRes = currentCanvasScaler.referenceResolution.x;
        if (replayViewerScript == null)
        {
            Debug.LogError("Assign the replay playback script to the seek", gameObject);
        }
    }

    public void ToggleSeekbarTimePreview(bool state)
    {
        mouseOverSeek = state;
        if (!state)
        {
            if (scrubbing)
            {
                // Don't exit the scrubing mode if we have already clicked
                return;
            }
        }
        seekBarTimePreviewTransform.gameObject.SetActive(state);
    }

    void Update()
    {
        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            if (scrubbing)
            {
                scrubbing = false;
                if (!mouseOverSeek)
                {
                    ToggleSeekbarTimePreview(false);
                }
            }
        }

        if (mouseOverSeek || scrubbing)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                scrubbing = true;
            }

            float projectedPreviewPercent = Mouse.current.position.ReadValue().x / Screen.width;

            // Set seekbar preview position
            float seekbarPosition = projectedPreviewPercent * referenceRes;
            float screenOverflow = seekBarTimePreviewTransform.sizeDelta.x * 0.5f;
            seekbarPosition = Mathf.Clamp(seekbarPosition,
                screenOverflow,
                referenceRes - screenOverflow);
            seekBarTimePreviewTransform.anchoredPosition = new Vector2(seekbarPosition, seekBarTimePreviewTransform.anchoredPosition.y);

            // Set seekbar preview timestamp
            float projectedSeconds = replayViewerScript.totalTime * projectedPreviewPercent;
            projectedSeconds = Mathf.Clamp(projectedSeconds, 0f, replayViewerScript.totalTime);
            TimeSpan t = TimeSpan.FromSeconds(projectedSeconds);
            string timestamp = $"{t.Hours:D2}:{t.Minutes:D2}:{t.Seconds:D2}";
            seekBarTimePreviewText.text = timestamp;
        }
    }

    public void ToggleScrubbing(bool state)
    {
        if (!state)
        {
            if (!replayViewerScript.pauseButtonStatus)
            {
                replayViewerScript.pauseReplay = false;
            }
        }
        else
        {
            replayViewerScript.pauseReplay = true;
        }
        replayViewerScript.scrubbing = state;
        replayViewerScript.PausePlayButtonVisualToggle();
    }
}
