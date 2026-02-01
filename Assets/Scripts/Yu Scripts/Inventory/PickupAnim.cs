using System.Collections;
using UnityEngine;

public class PickupAnim : MonoBehaviour
{
    [Header("Refs")]
    public SpriteRenderer sr;
    public Collider2D col;

    [Header("Anim")]
    public float duration = 0.35f;
    public float popScale = 1.25f;        
    public float liftY = 0.3f;           
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("VFX (optional)")]
    public ParticleSystem pickupVfxPrefab; 
    public bool spawnVfxAsChild = true;

    bool _playing;

    void Reset()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
    }

    public void PlayAndDisable(System.Action onDone = null)
    {
        if (_playing) return;
        StartCoroutine(CoPlay(onDone));
    }

    IEnumerator CoPlay(System.Action onDone)
    {
        _playing = true;

        
        if (col) col.enabled = false;

        
        ParticleSystem vfx = null;
        if (pickupVfxPrefab)
        {
            vfx = Instantiate(pickupVfxPrefab,
                transform.position,
                Quaternion.identity);

            if (spawnVfxAsChild) vfx.transform.SetParent(transform, true);
            vfx.Play();
        }

        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + Vector3.up * liftY;

        Vector3 startScale = transform.localScale;
        Vector3 peakScale = startScale * popScale;

        Color startColor = sr ? sr.color : Color.white;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float e = ease.Evaluate(k);

           
            transform.position = Vector3.Lerp(startPos, endPos, e);

            
            float s = (k < 0.5f)
                ? Mathf.Lerp(1f, popScale, k / 0.5f)
                : Mathf.Lerp(popScale, 0.9f, (k - 0.5f) / 0.5f);
            transform.localScale = startScale * s;

           
            if (sr)
            {
                Color c = startColor;
                c.a = Mathf.Lerp(1f, 0f, e);
                sr.color = c;
            }

            yield return null;
        }

        
        gameObject.SetActive(false);

       
        if (vfx && !spawnVfxAsChild) Destroy(vfx.gameObject, 2f);

        onDone?.Invoke();
    }
}
