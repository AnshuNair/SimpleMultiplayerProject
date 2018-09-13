using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MoveCard : MonoBehaviour
{	
	CardModel cardModel;
	Transform playedCard;

	void Start()
	{
		cardModel = this.gameObject.GetComponent<CardModel> ();
	}

	void OnMouseDown()
	{
		if (this.transform.parent.parent.name == "You")
			playedCard = GameObject.Find ("YourPlayZone").transform;
		
		else if (this.transform.parent.parent.name == "Opponent")
			playedCard = GameObject.Find ("EnemyPlayZone").transform;

		if (playedCard.childCount < 1) 
		{
			GameObject guidelines = GameObject.Find("GameStatus");
			Text message = guidelines.GetComponent<Text> ();
			message.text = "You played " + this.gameObject.name;
			cardModel.ToggleFace (false);
			this.transform.GetChild (0).gameObject.SetActive (false);
			this.transform.position = playedCard.position;
			this.transform.SetParent (playedCard);
		} 

		else
			Debug.Log ("You have already played a card");
	}

}
