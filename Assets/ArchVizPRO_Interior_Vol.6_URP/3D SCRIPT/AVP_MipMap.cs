using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class AVP_MipMap : MonoBehaviour
{



	public float target = -2f;
	public bool allTexturesOnStart;
	public Texture[] texturesToAdjust;

	void Start()
	{
		if (allTexturesOnStart)
		{
			foreach (Texture tex in (Texture[])Resources.FindObjectsOfTypeAll(typeof(Texture)))
			{
				tex.mipMapBias = target;
			}
		}
	}


	void Update()
	{
		foreach (Texture tex in texturesToAdjust)
		{
			tex.mipMapBias = target;
		}
	}

}