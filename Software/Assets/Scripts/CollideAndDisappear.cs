using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollideAndDisappear : MonoBehaviour
{
	public bool isTouched = false;
    private bool canBeTouched;
    public bool finishedVisual;
    private Color originalColor;
    public bool playSound;
    // Start is called before the first frame update
    void Start()
    {
        SetMaterialFade(GetComponent<Renderer>().material);
        this.GetComponent<Collider>().isTrigger = true;
        originalColor = this.GetComponent<Renderer>().material.color;
        canBeTouched = true;
    }

    // Update is called once per frame
    void Update()
    {
        if(UnityEngine.Input.GetKeyDown(KeyCode.Space))
        {
        	isTouched = true;
            StartCoroutine(FadeColor());
        }
    }

	void OnTriggerEnter(Collider other)
    {
    	if((other.GetComponent<Collider>().tag == "InteractiveObject") && canBeTouched)
    	{
    		isTouched = true;
            canBeTouched = false;
            StartCoroutine(FadeColor());
            if(playSound)
            {
                this.GetComponent<AudioSource>().Play();
            }
    	}

    }

    IEnumerator FadeColor()
    {
        Renderer rend = GetComponent<Renderer>();
        Color color = rend.material.color;

        while(color.a > 0.5f)
        {
            color.a -= Time.deltaTime * 0.8f;
            rend.material.color = color;
            yield return null;
        }
        this.finishedVisual = true;        
    }

    void SetMaterialFade(Material mat)
    {
        mat.SetFloat("_Mode", 2); // 2 = Fade, 3 = Transparent
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
    }

}
