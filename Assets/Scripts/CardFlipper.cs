using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardFlipper : MonoBehaviour 
{
	SpriteRenderer spriteRenderer;
	CardModel cardModel;

	public AnimationCurve scaleCurve;
	float duration = 0.5f;

	void Awake()
	{
		spriteRenderer = GetComponent<SpriteRenderer> ();
		cardModel = GetComponent<CardModel> ();
	}

	public void FlipCard()
	{
		StartCoroutine (Flip ());
	}

	IEnumerator Flip()
	{
		spriteRenderer.sprite = cardModel.cardBack;
		float time = 0f;

		while (time <= 1f) 
		{
			float scale = scaleCurve.Evaluate (time);
			time = time + Time.deltaTime / duration;
			Vector3 customScale = transform.localScale;
			customScale.x = scale;
			transform.localScale = customScale;

			if (time >= 0.5f) 
			{
				spriteRenderer.sprite = cardModel.frontFace;
			}

			yield return new WaitForFixedUpdate ();
		}
	}
}
