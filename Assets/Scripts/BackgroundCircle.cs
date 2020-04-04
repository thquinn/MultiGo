using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundCircle : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;

    int frames;

    // Start is called before the first frame update
    void Start()
    {
        Color c = spriteRenderer.color;
        c.a = 0;
        spriteRenderer.color = c;
        frames = UnityEngine.Random.Range(450, 750);
    }
    
    void Update()
    {
        frames--;
        if (frames <= 0) {
            Color c = spriteRenderer.color;
            c.a -= .01f;
            if (c.a <= 0) {
                Destroy(gameObject);
                return;
            }
            spriteRenderer.color = c;
        } else if (spriteRenderer.color.a < 1) {
            Color c = spriteRenderer.color;
            c.a += .01f;
            spriteRenderer.color = c;
        }
    }
}
