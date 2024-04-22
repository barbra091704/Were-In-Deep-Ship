using System.Collections;
using UnityEngine.UI;
using UnityEngine;

public class FadeColor : MonoBehaviour
{
    public bool activeStartState;
    public Image fadeImage;
    public Color startColor;
    public Color endColor;

    public void Awake()
    {
        fadeImage.enabled = activeStartState;
    }

    public void Fade(float fadeTime, float waitTime, float fadeOutTime)
    {
        StopCoroutine(nameof(FadeRoutine));
        StartCoroutine(FadeRoutine(fadeTime, waitTime, fadeOutTime));
    }

private IEnumerator FadeRoutine(float fadeInTime, float waitTime, float fadeOutTime)
{
    fadeImage.enabled = true;
    GUIManager.Singleton.AddGUI();
    // Fade in
    float timer = 0;
    while (timer < fadeInTime)
    {
        timer += Time.deltaTime;
        fadeImage.color = Color.Lerp(startColor, endColor, timer / fadeInTime);
        yield return null;
    }

    // Wait
    yield return new WaitForSeconds(waitTime);

    // Fade out
    timer = 0; // Reset timer for fade out
    while (timer < fadeOutTime)
    {
        timer += Time.deltaTime;
        fadeImage.color = Color.Lerp(endColor, startColor, timer / fadeOutTime);
        yield return null;
    }
    GUIManager.Singleton.RemoveGUI();
    fadeImage.enabled = false;
}

}
