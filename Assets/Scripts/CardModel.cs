using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardModel : MonoBehaviour 
{
	SpriteRenderer spriteRenderer;
	public Sprite frontFace;
	public Sprite cardBack;

	public int cardIndex;

	public void ToggleFace(bool showFace)
	{
		if (showFace)
			spriteRenderer.sprite = frontFace;

		else 
		{
			spriteRenderer.sprite = cardBack;
		}
	}

	void Awake ()
	{
		spriteRenderer = GetComponent<SpriteRenderer> ();
	}
}
