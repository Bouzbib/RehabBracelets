using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollideAndDisappear : MonoBehaviour
{
	public bool isTouched = false;
    public bool finishedVisual;
    private Color originalColor;
    // Start is called before the first frame update
    void Start()
    {
        SetMaterialFade(GetComponent<Renderer>().material);
        this.GetComponent<Collider>().isTrigger = true;
        originalColor = this.GetComponent<Renderer>().material.color;
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
    	if(other.GetComponent<Collider>().tag == "InteractiveObject")
    	{
    		this.isTouched = true;
            StartCoroutine(FadeColor());
    	}

    }

    IEnumerator FadeColor()
    {
        Renderer rend = GetComponent<Renderer>();
        Color color = rend.material.color;

        while(color.a > 0.5f)
        {
            color.a -= Time.deltaTime * 0.8f; // control speed here
            rend.material.color = color;
            yield return null; // wait one frame, then loop
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
