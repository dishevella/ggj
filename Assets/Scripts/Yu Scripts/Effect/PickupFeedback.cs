using UnityEngine;

public class PickupFeedback : MonoBehaviour
{
    public static PickupFeedback I { get; private set; }

    [Header("World VFX")]
    public ParticleSystem pickupVfxPrefab;

    [Header("UI Toast")]
    public Canvas uiCanvas;                 
    public PickupToastUI toastPrefab;       
    public Vector2 toastOffset = new Vector2(30, -30); 

    void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
    }

    public void Play(Sprite icon, Vector3 worldPos, Vector2 mouseScreenPos)
    {
        
        if (pickupVfxPrefab)
        {
            var vfx = Instantiate(pickupVfxPrefab, worldPos, Quaternion.identity);
            vfx.Play();
            Destroy(vfx.gameObject, 2f);
        }

        
        if (uiCanvas && toastPrefab)
        {
            var toast = Instantiate(toastPrefab, uiCanvas.transform, false);
            toast.Play(icon, mouseScreenPos + toastOffset, uiCanvas);
        }
    }
}
