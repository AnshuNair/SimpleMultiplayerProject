using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class PlayerHud : NetworkBehaviour
{
    Transform playerSpawn;
    Transform enemySpawn;
    Transform gameStatus;
    Transform yPlayZone;
    Transform ePlayZone;
    Transform yDiscardZone;
    Transform eDiscardZone;
    GameObject roundObj;
    Text roundText;
    Text playerName;
    Text guidelines;
    bool oneTimeSpawn = true;
    public GameObject headsCoin;
    public GameObject tailsCoin;
    public GameObject nameField;
    public GameObject choiceObject;
    public GameObject emperorHand;
    public GameObject slaveHand;

    [SyncVar]
    bool serverWin = false;

    [SyncVar]
    bool clientWin = false;

    [SyncVar]
    bool handChoice = false;

    [SyncVar]
    bool handsDisplayed = false;

    [SyncVar]
    bool emperorChoice = false;

    [SyncVar]
    bool slaveChoice = false;

    [SyncVar]
    bool hostTurn = false;

    [SyncVar]
    bool clientTurn = false;

    [SyncVar]
    bool clientPlayedOne = false;

    [SyncVar]
    bool syncForServer = false;

    [SyncVar]
    bool syncForClient = false;

    [SyncVar]
    bool hostPlayedOne = false;

    [SyncVar]
    bool emperorPlayed = false;

    [SyncVar]
    bool slavePlayed = false;

    [SyncVar]
    int hostCard; // 1 = Citizen, 2 = Emperor, 3 = Slave

    [SyncVar]
    int clientCard; // 1 = Citizen, 2 = Emperor, 3 = Slave

    [SyncVar]
    int hostScore = 0;

    [SyncVar]
    int clientScore = 0;

    [SyncVar]
    int whoPlayedOne = 0;

    [SyncVar]
    bool p1Heads;

    [SyncVar]
    bool p2Heads;

    [SyncVar]
    bool nameEntered = true;

    [SyncVar]
    string input;

    string clientMessage;
    string message;

    [SyncVar]
    bool whichAnim;

    GameObject inputObject;
    InputField inputField;
    Transform gameCanvas;

    void Awake()
    {
        playerSpawn = GameObject.Find("SpawnPointOne").transform;
        enemySpawn = GameObject.Find("SpawnPointTwo").transform;
        gameStatus = GameObject.Find("GameStatus").transform;
        gameCanvas = GameObject.Find("GameCanvas").transform;
        yPlayZone = GameObject.Find("YourPlayZone").transform;
        ePlayZone = GameObject.Find("EnemyPlayZone").transform;
        yDiscardZone = GameObject.Find("YourDiscardZone").transform;
        eDiscardZone = GameObject.Find("EnemyDiscardZone").transform;
        roundObj = GameObject.Find("RoundText");

        playerName = null;
    }

    void Start()
    {
        guidelines = gameStatus.GetComponent<Text>();
        roundText = roundObj.GetComponent<Text>();

        if (NetworkManager.singleton.numPlayers > 1)
            oneTimeSpawn = false;

        if (GameObject.Find("PlayerConnection(Clone)") != null)
        {
            StartCoroutine(PositionPlayers());
        }

        StartCoroutine(CoinToss());
    }

    IEnumerator PositionPlayers()
    {
        bool spawnForHost = isServer && oneTimeSpawn;

        playerName = this.transform.GetChild(0).gameObject.GetComponent<Text>();
        if (hasAuthority)
        {
            StartCoroutine(EnterPlayerName());

            yield return new WaitUntil(() => nameEntered == false);
            this.name = "You";
            this.transform.SetParent(playerSpawn);
            this.transform.position = playerSpawn.position;
            Destroy(inputObject);
        }

        else if (!hasAuthority)
        {
            if (spawnForHost)
            {
                StartCoroutine(EnterPlayerName());

                yield return new WaitUntil(() => nameEntered == false);
                this.name = "You";
                this.transform.SetParent(playerSpawn);
                this.transform.position = playerSpawn.position;
                oneTimeSpawn = false;
                Destroy(inputObject);
            }

            else
            {
                yield return new WaitUntil(() => nameEntered == false);
                this.name = "Opponent";
                this.transform.SetParent(enemySpawn);
                this.transform.position = enemySpawn.position;
                playerName.text = input;
            }
        }

        RectTransform rt = this.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector3(0, 0, 0);
        this.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
    }

    IEnumerator EnterPlayerName()
    {
        guidelines.text = "Please enter your player name and click the [Set Name] button";
        inputObject = Instantiate(nameField);
        inputObject.transform.SetParent(gameCanvas.transform);
        RectTransform rt = inputObject.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector3(0, 0, 0);
        inputObject.transform.localScale = new Vector3(1, 1, 1);
        inputField = inputObject.transform.GetChild(0).GetComponent<InputField>();
        Button nameButton = inputObject.transform.GetChild(1).GetComponent<Button>();
        nameButton.onClick.AddListener(ButtonForName);
        yield return new WaitUntil(() => nameEntered == false);
        guidelines.text = "";
    }

    public void ButtonForName()
    {
        CmdOnNameChange(inputField.text);
        CmdChangeNameBool(false);
        playerName.text = inputField.text;
        nameEntered = false;
    }

    [Command]
    void CmdOnNameChange(string newName)
    {
        input = newName;
        playerName.text = input;
    }

    [Command]
    void CmdChangeNameBool(bool nameChange)
    {
        nameEntered = nameChange;
    }

    bool BothHaveNames()
    {
        return (GameObject.Find("You") != null && GameObject.Find("Opponent") != null);
    }

    IEnumerator CoinToss()
    {
        yield return new WaitUntil(() => BothHaveNames() == true);
        Text guidelines = gameStatus.GetComponent<Text>();
        //guidelines.text = "Both Players have joined! Assigning players Heads and Tails";
        //yield return new WaitForSeconds (3);
        if (!hasAuthority)
        {
            if (isServer)
            {
                CmdAssignHT();
                CmdDecideOutcome();
                CmdAnimateCoin(whichAnim);
            }
        }

        yield return new WaitForSeconds(3.8f);

        if (!hasAuthority)
        {
            CmdMessageAfterAnim(whichAnim);
        }

        CmdDestroyCoin(whichAnim);

        if (!hasAuthority)
        {
            if (isServer)
                CmdPresentChoice();
        }

        yield return new WaitUntil(() => handChoice == true);

        DestroyChoiceObj();

        if (!hasAuthority)
        {
            if (isServer)
                CmdDisplayHands();
        }

        yield return new WaitUntil(() => handsDisplayed == true);

        StartCoroutine(StartTurns());
    }

    [ClientRpc]
    void RpcAssignForClient(bool headsOrTails)
    {
        if (!isServer)
        {
            if (headsOrTails)
            {
                clientMessage = "You are TAILS";
            }

            else
            {
                clientMessage = "You are HEADS";
            }

            guidelines.text = clientMessage;
        }
    }

    [Command]
    void CmdAssignHT()
    {
        if (Random.value < 0.5f)
        {
            p1Heads = true;
            message = "You are HEADS";
        }
        else
        {
            p2Heads = true;
            message = "You are TAILS";
        }

        guidelines.text = message;
        RpcAssignForClient(p1Heads);
    }

    [Command]
    void CmdDecideOutcome()
    {
        if (Random.value < 0.5f)
            whichAnim = true;
        else
            whichAnim = false;
    }

    [Command]
    void CmdAnimateCoin(bool whichCoin)
    {
        GameObject spawnedCoin;

        if (whichCoin)
            spawnedCoin = Instantiate(headsCoin);
        else
            spawnedCoin = Instantiate(tailsCoin);
        NetworkServer.Spawn(spawnedCoin);
        Animator hAnim = spawnedCoin.GetComponent<Animator>();

        if (whichCoin)
            hAnim.Play("HeadsTossAnim");
        if (!whichCoin)
            hAnim.Play("TailsTossAnim");
    }

    [ClientRpc]
    void RpcMessageAfterAnim(bool headOrTail)
    {
        if (!isServer)
        {
            if (headOrTail)
            {
                if (p1Heads)
                {
                    clientMessage = "Your opponent has won the coin toss. They are deciding which hand they want...";
                }

                if (p2Heads)
                {
                    clientMessage = "You have won the coin toss. You may now decide which hand you want...";
                }
            }

            if (!headOrTail)
            {
                if (p1Heads)
                {
                    clientMessage = "Your opponent has won the coin toss. They are deciding which hand they want...";
                }

                if (p2Heads)
                {
                    clientMessage = "You have won the coin toss. You may now decide which hand you want...";
                }
            }

            guidelines.text = clientMessage;
        }
    }

    [Command]
    void CmdMessageAfterAnim(bool headsOrTails)
    {
        if (headsOrTails)
        {
            if (p1Heads)
            {
                message = "You have won the coin toss. You may now decide which hand you want...";
                serverWin = true;
            }

            if (p2Heads)
            {
                message = "Your opponent has won the coin toss. They are deciding which hand they want...";
                clientWin = true;
            }
        }

        if (!headsOrTails)
        {
            if (p1Heads)
            {
                message = "Your opponent has won the coin toss. They are deciding which hand they want...";
                clientWin = true;
            }

            if (p2Heads)
            {
                message = "You have won the coin toss. You may now decide which hand you want...";
                serverWin = true;
            }
        }

        guidelines.text = message;
        RpcMessageAfterAnim(headsOrTails);
    }

    [Command]
    void CmdDestroyCoin(bool headsOrTails)
    {
        if (headsOrTails)
        {
            GameObject hCoin = GameObject.Find("HeadsCoin(Clone)");
            Destroy(hCoin);
        }
        if (!headsOrTails)
        {
            GameObject tCoin = GameObject.Find("TailsCoin(Clone)");
            Destroy(tCoin);
        }
    }

    [Command]
    void CmdPresentChoice()
    {
        if (serverWin)
            PresentChoice();

        if (clientWin)
            RpcPresentChoice();
        //update appropriate variable in gcScript to set order of hand rotations.
    }

    [ClientRpc]
    void RpcPresentChoice()
    {
        if (!isServer)
        {
            //if (clientWin)
            PresentChoice();
            //update appropriate variable in gcScript to set order of hand rotations.
        }
    }

    void PresentChoice()
    {
        GameObject spawnedChoice = Instantiate(choiceObject);
        spawnedChoice.transform.SetParent(gameCanvas.transform);
        RectTransform rt = spawnedChoice.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector3(0, 0, 0);
        spawnedChoice.transform.localScale = new Vector3(1, 1, 1);

        GameObject emperorObj = spawnedChoice.transform.GetChild(0).gameObject;
        GameObject empButtonObj = emperorObj.transform.GetChild(0).gameObject;
        Button emperorButton = empButtonObj.GetComponent<Button>();
        emperorButton.onClick.AddListener(ButtonForEmperor);

        GameObject slaveObj = spawnedChoice.transform.GetChild(1).gameObject;
        GameObject slaveButtonObj = slaveObj.transform.GetChild(0).gameObject;
        Button slaveButton = slaveButtonObj.GetComponent<Button>();
        slaveButton.onClick.AddListener(ButtonForSlave);
    }

    [Command]
    void CmdButtonForEmperor()
    {
        emperorChoice = true;
        handChoice = true;
        RpcButtonForEmperor();
    }

    [Command]
    void CmdButtonForSlave()
    {
        slaveChoice = true;
        handChoice = true;
        RpcButtonForSlave();
    }

    void ButtonForEmperor()
    {
        CmdButtonForEmperor();
    }

    void ButtonForSlave()
    {
        CmdButtonForSlave();
    }

    [ClientRpc]
    void RpcButtonForSlave()
    {
        if (!isServer)
        {
            slaveChoice = true;
            handChoice = true;
        }
    }

    [ClientRpc]
    void RpcButtonForEmperor()
    {
        if (!isServer)
        {
            emperorChoice = true;
            handChoice = true;
        }
    }

    void DestroyChoiceObj()
    {
        if (GameObject.Find("ChoiceObject(Clone)") != null)
            Destroy(GameObject.Find("ChoiceObject(Clone)"));
    }

    [ClientRpc]
    void RpcDisplayHands()
    {
        if (!isServer)
        {
            GameObject eHand = GameObject.Find("EmperorHand(Clone)");
            eHand.transform.SetParent(gameCanvas);
            eHand.transform.localScale = new Vector3(1, 1, 1);
            RectTransform emperorRt = eHand.GetComponent<RectTransform>();

            GameObject sHand = GameObject.Find("SlaveHand(Clone)");
            sHand.transform.SetParent(gameCanvas);
            sHand.transform.localScale = new Vector3(1, 1, 1);
            RectTransform slaveRt = sHand.GetComponent<RectTransform>();

            if (serverWin)
            {
                if (emperorChoice)
                {
                    emperorRt.anchoredPosition = new Vector3(-150, 275, 0);
                    eHand.transform.SetParent(GameObject.Find("Opponent").transform);
                    slaveRt.anchoredPosition = new Vector3(-150, -275, 0);
                    sHand.transform.SetParent(GameObject.Find("You").transform);
                    clientMessage = "Your opponent has chosen to be the Emperor";
                }

                else if (slaveChoice)
                {
                    slaveRt.anchoredPosition = new Vector3(-150, 275, 0);
                    sHand.transform.SetParent(GameObject.Find("Opponent").transform);
                    emperorRt.anchoredPosition = new Vector3(-150, -275, 0);
                    eHand.transform.SetParent(GameObject.Find("You").transform);
                    clientMessage = "Your opponent has chosen to be the Slave";
                }
            }

            else if (clientWin)
            {
                if (emperorChoice)
                {
                    emperorRt.anchoredPosition = new Vector3(-150, -275, 0);
                    eHand.transform.SetParent(GameObject.Find("You").transform);
                    slaveRt.anchoredPosition = new Vector3(-150, 275, 0);
                    sHand.transform.SetParent(GameObject.Find("Opponent").transform);
                    clientMessage = "You have chosen to be the Emperor";
                }

                else if (slaveChoice)
                {
                    slaveRt.anchoredPosition = new Vector3(-150, -275, 0);
                    sHand.transform.SetParent(GameObject.Find("You").transform);
                    emperorRt.anchoredPosition = new Vector3(-150, 275, 0);
                    eHand.transform.SetParent(GameObject.Find("Opponent").transform);
                    clientMessage = "You have chosen to be the Slave";
                }
            }

            guidelines.text = clientMessage;
            handsDisplayed = true;
        }
    }

    [Command]
    void CmdDisplayHands()
    {
        GameObject eHand = Instantiate(emperorHand);
        NetworkServer.Spawn(eHand);
        eHand.transform.SetParent(gameCanvas);
        eHand.transform.localScale = new Vector3(1, 1, 1);
        RectTransform emperorRt = eHand.GetComponent<RectTransform>();

        GameObject sHand = Instantiate(slaveHand);
        NetworkServer.Spawn(sHand);
        sHand.transform.SetParent(gameCanvas);
        sHand.transform.localScale = new Vector3(1, 1, 1);
        RectTransform slaveRt = sHand.GetComponent<RectTransform>();

        if (serverWin)
        {
            if (emperorChoice)
            {
                emperorRt.anchoredPosition = new Vector3(-150, -275, 0);
                eHand.transform.SetParent(GameObject.Find("You").transform);
                slaveRt.anchoredPosition = new Vector3(-150, 275, 0);
                sHand.transform.SetParent(GameObject.Find("Opponent").transform);
                message = "You have chosen to be the Emperor";

            }

            else if (slaveChoice)
            {
                slaveRt.anchoredPosition = new Vector3(-150, -275, 0);
                sHand.transform.SetParent(GameObject.Find("You").transform);
                emperorRt.anchoredPosition = new Vector3(-150, 275, 0);
                eHand.transform.SetParent(GameObject.Find("Opponent").transform);
                message = "You have chosen to be the Slave";
            }
        }

        else if (clientWin)
        {
            if (emperorChoice)
            {
                emperorRt.anchoredPosition = new Vector3(-150, 275, 0);
                eHand.transform.SetParent(GameObject.Find("Opponent").transform);
                slaveRt.anchoredPosition = new Vector3(-150, -275, 0);
                sHand.transform.SetParent(GameObject.Find("You").transform);
                message = "Your opponent has chosen to be the Emperor";
            }

            else if (slaveChoice)
            {
                slaveRt.anchoredPosition = new Vector3(-150, 275, 0);
                sHand.transform.SetParent(GameObject.Find("Opponent").transform);
                emperorRt.anchoredPosition = new Vector3(-150, -275, 0);
                eHand.transform.SetParent(GameObject.Find("You").transform);
                message = "Your opponent has chosen to be the Slave";
            }
        }
        guidelines.text = message;
        RpcDisplayHands();
        handsDisplayed = true;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    IEnumerator StartTurns()
    {
        if (!hasAuthority)
        {
            if (isServer)
                CmdWhoseTurn();
        }

        for (int round = 1; round < 5; round++)
        {
            whoPlayedOne = 0; // 1 means server, 2 means client;
            roundText.text = "Round: " + round.ToString();
            emperorPlayed = false;
            slavePlayed = false;

            while (emperorPlayed == false && slavePlayed == false)
            {
                if (!hasAuthority)
                {
                    if (isServer)
                        CmdGrantPlayAbility();
                }

                Debug.Log("Waiting for the first card to be played");
                yield return new WaitUntil(() => (DidClientPlayOne() || DidHostPlayOne()) == true);

                if (!hasAuthority)
                {
                    if (isServer)
                    {
                        if (DidClientPlayOne())
                        {
                            Debug.Log("After the waitUntil. The CLIENT played a card");
                            CmdCheckPlayZones();
                            CmdChangeTurn();
                            whoPlayedOne = 2;
                        }
                    }
                }

                if (!hasAuthority)
                {
                    if (isServer)
                    {
                        if (DidHostPlayOne())
                        {
                            Debug.Log("After the waitUntil. The HOST played a card");
                            CmdCheckPlayZones();
                            CmdChangeTurn();
                            whoPlayedOne = 1;
                        }
                    }
                }

                yield return new WaitUntil(() => (syncForServer || syncForClient) == true);

                Debug.Log("After the first Sync");

                if (!hasAuthority)
                {
                    if (isServer)
                    {
                        CmdGrantPlayAbility();
                    }
                }

                syncForServer = false;
                syncForClient = false;

                if (whoPlayedOne == 2)
                    Debug.Log("The client has played a card. Waiting for host to play a card");

                if (whoPlayedOne == 1)
                    Debug.Log("The host has played a card. Waiting for client to play a card");

                Debug.Log("Waiting for the second card to be played");

                yield return new WaitUntil(() => (DidClientPlayOne() && DidHostPlayOne()) == true);

                if (!hasAuthority)
                {
                    if (whoPlayedOne == 1)
                    {
                        if (isServer)
                        {
                            if (DidClientPlayOne())
                            {
                                Debug.Log("After the waitUntil. The CLIENT played the second card");
                                CmdCheckPlayZones();
                            }
                        }
                    }

                }

                if (!hasAuthority)
                {
                    if (whoPlayedOne == 2)
                    {
                        if (isServer)
                        {
                            if (DidHostPlayOne())
                            {
                                Debug.Log("After the waitUntil. The HOST played the second card");
                                CmdCheckPlayZones();
                            }
                        }
                    }
                }

                yield return new WaitUntil(() => (syncForServer || syncForClient) == true);

                Debug.Log("After the second Sync");

                if (!hasAuthority)
                {
                    if (isServer)
                    {
                        CmdTurnResult();
                    }
                }

                if (!isServer)
                {
                    if (hasAuthority)
                    {
                        if (yPlayZone.GetChild(0).name == "Emperor" || ePlayZone.GetChild(0).name == "Emperor")
                            emperorPlayed = true;

                        if (yPlayZone.GetChild(0).name == "Slave" || ePlayZone.GetChild(0).name == "Slave")
                            slavePlayed = true;

                        Debug.Log("Emperor Played: " + emperorPlayed);
                        Debug.Log("Slave Played: " + slavePlayed);
                    }
                }

                yield return new WaitForSeconds(2.5f);

                if (emperorPlayed == false && slavePlayed == false)
                {
                    if (!hasAuthority)
                    {
                        if (isServer)
                        {
                            Debug.Log("Trying to put the Citizens in the discard pile");
                            CmdAfterADraw();
                        }
                    }
                }

                else if (emperorPlayed == true || slavePlayed == true)
                {
                    if (!hasAuthority)
                    {
                        if (isServer)
                        {
                            CmdDestroyHands();
                            CmdChangeTurn();
                            CmdDisplayHandsAgain();
                        }
                    }
                }

                if (!hasAuthority && isServer)
                    CmdResetBools();

                if (!hasAuthority)
                {
                    if (isServer)
                    {
                        CmdChangeTurn();
                    }
                }
            }
        }
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    [ClientRpc]
    void RpcWhoseTurn()
    {
        if (!isServer)
        {
            if ((serverWin && slaveChoice) || (clientWin && emperorChoice))
                hostTurn = true;
            if ((clientWin && slaveChoice) || (serverWin && emperorChoice))
                clientTurn = true;
        }
    }

    [Command]
    void CmdWhoseTurn()
    {
        if ((serverWin && slaveChoice) || (clientWin && emperorChoice))
            hostTurn = true;
        if ((clientWin && slaveChoice) || (serverWin && emperorChoice))
            clientTurn = true;

        RpcWhoseTurn();
    }

    [ClientRpc]
    void RpcGrantPlayAbility()
    {
        if (!isServer)
        {
            GameObject yourHand = GameObject.Find("You").transform.GetChild(2).gameObject;
            GameObject enemyHand = GameObject.Find("Opponent").transform.GetChild(2).gameObject;

            if (hostTurn)
            {
                for (int i = 0; i < enemyHand.transform.childCount; i++)
                {
                    enemyHand.transform.GetChild(i).GetChild(0).gameObject.SetActive(true);
                }

                for (int i = 0; i < yourHand.transform.childCount; i++)
                {
                    if (yourHand.transform.GetChild(i).GetComponent<MoveCard>() != null)
                    {
                        MoveCard moveCard = yourHand.transform.GetChild(i).GetComponent<MoveCard>();
                        Destroy(moveCard);
                    }

                    yourHand.transform.GetChild(i).GetChild(0).gameObject.SetActive(false);
                }

                clientMessage = "It's your opponent's turn. They are playing a card...";
            }

            else if (clientTurn)
            {
                for (int i = 0; i < enemyHand.transform.childCount; i++)
                {
                    if (enemyHand.transform.GetChild(i).GetComponent<MoveCard>() != null)
                    {
                        MoveCard moveCard = enemyHand.transform.GetChild(i).GetComponent<MoveCard>();
                        Destroy(moveCard);
                    }

                    enemyHand.transform.GetChild(i).GetChild(0).gameObject.SetActive(false);
                }

                for (int i = 0; i < yourHand.transform.childCount; i++)
                {
                    if (yourHand.transform.GetChild(i).GetComponent<MoveCard>() == null)
                    {
                        yourHand.transform.GetChild(i).gameObject.AddComponent<MoveCard>();
                    }

                    yourHand.transform.GetChild(i).GetChild(0).gameObject.SetActive(true);
                }

                clientMessage = "It's your turn. Please play a card...";
            }

            guidelines.text = clientMessage;
        }
    }

    [Command]
    void CmdGrantPlayAbility()
    {
        GameObject yourHand = GameObject.Find("You").transform.GetChild(2).gameObject;
        GameObject enemyHand = GameObject.Find("Opponent").transform.GetChild(2).gameObject;

        if (hostTurn)
        {
            for (int i = 0; i < yourHand.transform.childCount; i++)
            {
                if (yourHand.transform.GetChild(i).GetComponent<MoveCard>() == null)
                {
                    yourHand.transform.GetChild(i).gameObject.AddComponent<MoveCard>();
                }

                yourHand.transform.GetChild(i).GetChild(0).gameObject.SetActive(true);
            }

            for (int i = 0; i < enemyHand.transform.childCount; i++)
            {
                if (enemyHand.transform.GetChild(i).GetComponent<MoveCard>() != null)
                {
                    MoveCard moveCard = enemyHand.transform.GetChild(i).GetComponent<MoveCard>();
                    Destroy(moveCard);
                }

                enemyHand.transform.GetChild(i).GetChild(0).gameObject.SetActive(false);
            }

            message = "It's your turn. Please play a card...";
        }

        else if (clientTurn)
        {
            for (int i = 0; i < yourHand.transform.childCount; i++)
            {
                if (yourHand.transform.GetChild(i).GetComponent<MoveCard>() != null)
                {
                    MoveCard moveCard = yourHand.transform.GetChild(i).GetComponent<MoveCard>();
                    Destroy(moveCard);
                }

                yourHand.transform.GetChild(i).GetChild(0).gameObject.SetActive(false);
            }

            for (int i = 0; i < enemyHand.transform.childCount; i++)
            {
                enemyHand.transform.GetChild(i).GetChild(0).gameObject.SetActive(true);
            }

            message = "It's your opponent's turn. They are playing a card...";
        }

        guidelines.text = message;
        RpcGrantPlayAbility();
    }

    [ClientRpc]
    void RpcChangeTurn(bool forHost, bool forClient)
    {
        hostTurn = forHost;
        clientTurn = forClient;
    }

    [Command]
    void CmdChangeTurn()
    {
        if (hostTurn)
        {
            hostTurn = false;
            clientTurn = true;
        }

        else if (clientTurn)
        {
            clientTurn = false;
            hostTurn = true;
        }

        RpcChangeTurn(hostTurn, clientTurn);
    }

    bool DidHostPlayOne()
    {
        CmdHostPlayedOne();

        if (hostPlayedOne)
            return hostPlayedOne;
        else
            return false;
    }

    bool DidClientPlayOne()
    {
        OneCardPlayed();

        if (clientPlayedOne)
            return clientPlayedOne;
        else
            return false;
    }

    [Command]
    void CmdClientPlayedOne(bool didClient)
    {
        if (didClient)
            clientPlayedOne = true;
    }

    //[ClientRpc]
    void OneCardPlayed()
    {
        if (!isServer && hasAuthority)
        {
            if (yPlayZone.childCount == 1)
                clientPlayedOne = true;

            Debug.Log("Inside RpcOneCardPlayed. clientPlayedOne: " + clientPlayedOne);

            CmdClientPlayedOne(clientPlayedOne);
        }
    }

    [Command]
    void CmdHostPlayedOne()
    {
        if (yPlayZone.childCount == 1)
            hostPlayedOne = true;

        Debug.Log("Inside CmdHostPlayedOne. hostPlayedOne: " + hostPlayedOne);

        //RpcOneCardPlayed ();
    }

    [ClientRpc]
    void RpcCheckPlayZones()
    {
        if (!isServer && hasAuthority)
        {
            Debug.Log("Changing the clientCard integer. ClientTurn: " + clientTurn);

            if (clientTurn)
            {
                if (yPlayZone.childCount == 1)
                {
                    if (yPlayZone.GetChild(0).name == "Citizen")
                        clientCard = 1;
                    else if (yPlayZone.GetChild(0).name == "Emperor")
                        clientCard = 2;
                    else if (yPlayZone.GetChild(0).name == "Slave")
                        clientCard = 3;
                }

                CmdSyncPlayForServer(clientCard);
            }
        }
    }

    [Command]
    void CmdCheckPlayZones()
    {
        Debug.Log("In CmdCheckPlayZones. HostTurn: " + hostTurn);

        if (hostTurn)
        {
            if (yPlayZone.childCount == 1)
            {
                if (yPlayZone.GetChild(0).name == "Citizen")
                    hostCard = 1;
                else if (yPlayZone.GetChild(0).name == "Emperor")
                    hostCard = 2;
                else if (yPlayZone.GetChild(0).name == "Slave")
                    hostCard = 3;
            }

            RpcSyncPlayForClient(hostCard);
        }

        RpcCheckPlayZones();

    }

    [Command]
    void CmdSyncPlayForServer(int whichCard)
    {
        Debug.Log("Trying to sync card. ClientCard is: " + whichCard);

        GameObject enemyHand = GameObject.Find("Opponent").transform.GetChild(2).gameObject;

        if (whichCard == 1)
        {
            for (int i = 0; i < enemyHand.transform.childCount; i++)
            {
                if (enemyHand.transform.GetChild(i).name == "Citizen")
                {
                    GameObject cardToMove = enemyHand.transform.GetChild(i).gameObject;
                    cardToMove.transform.GetChild(0).gameObject.SetActive(false);
                    CardModel cardModel = cardToMove.GetComponent<CardModel>();
                    cardModel.ToggleFace(false);
                    cardToMove.transform.position = ePlayZone.position;
                    cardToMove.transform.SetParent(ePlayZone);
                    break;
                }
            }
        }

        if (whichCard == 2)
        {
            for (int i = 0; i < enemyHand.transform.childCount; i++)
            {
                if (enemyHand.transform.GetChild(i).name == "Emperor")
                {
                    GameObject cardToMove = enemyHand.transform.GetChild(i).gameObject;
                    cardToMove.transform.GetChild(0).gameObject.SetActive(false);
                    CardModel cardModel = cardToMove.GetComponent<CardModel>();
                    cardModel.ToggleFace(false);
                    cardToMove.transform.position = ePlayZone.position;
                    cardToMove.transform.SetParent(ePlayZone);
                }
            }
        }

        if (whichCard == 3)
        {
            for (int i = 0; i < enemyHand.transform.childCount; i++)
            {
                if (enemyHand.transform.GetChild(i).name == "Slave")
                {
                    GameObject cardToMove = enemyHand.transform.GetChild(i).gameObject;
                    cardToMove.transform.GetChild(0).gameObject.SetActive(false);
                    CardModel cardModel = cardToMove.GetComponent<CardModel>();
                    cardModel.ToggleFace(false);
                    cardToMove.transform.position = ePlayZone.position;
                    cardToMove.transform.SetParent(ePlayZone);
                }
            }
        }

        syncForServer = true;
    }

    [ClientRpc]
    void RpcSyncPlayForClient(int whichCard)
    {
        if (!isServer)
        {
            Debug.Log("Trying to sync card for client");

            GameObject enemyHand = GameObject.Find("Opponent").transform.GetChild(2).gameObject;

            if (whichCard == 1)
            {
                for (int i = 0; i < enemyHand.transform.childCount; i++)
                {
                    if (enemyHand.transform.GetChild(i).name == "Citizen")
                    {
                        GameObject cardToMove = enemyHand.transform.GetChild(i).gameObject;
                        cardToMove.transform.GetChild(0).gameObject.SetActive(false);
                        CardModel cardModel = cardToMove.GetComponent<CardModel>();
                        cardModel.ToggleFace(false);
                        cardToMove.transform.position = ePlayZone.position;
                        cardToMove.transform.SetParent(ePlayZone);
                        break;
                    }
                }
            }

            if (whichCard == 2)
            {
                for (int i = 0; i < enemyHand.transform.childCount; i++)
                {
                    if (enemyHand.transform.GetChild(i).name == "Emperor")
                    {
                        GameObject cardToMove = enemyHand.transform.GetChild(i).gameObject;
                        cardToMove.transform.GetChild(0).gameObject.SetActive(false);
                        CardModel cardModel = cardToMove.GetComponent<CardModel>();
                        cardModel.ToggleFace(false);
                        cardToMove.transform.position = ePlayZone.position;
                        cardToMove.transform.SetParent(ePlayZone);
                    }
                }
            }

            if (whichCard == 3)
            {
                for (int i = 0; i < enemyHand.transform.childCount; i++)
                {
                    if (enemyHand.transform.GetChild(i).name == "Slave")
                    {
                        GameObject cardToMove = enemyHand.transform.GetChild(i).gameObject;
                        cardToMove.transform.GetChild(0).gameObject.SetActive(false);
                        CardModel cardModel = cardToMove.GetComponent<CardModel>();
                        cardModel.ToggleFace(false);
                        cardToMove.transform.position = ePlayZone.position;
                        cardToMove.transform.SetParent(ePlayZone);
                    }
                }
            }

            syncForClient = true;
            CmdSyncBoolSyncForClient(syncForClient);
        }
    }

    [Command]
    void CmdSyncBoolSyncForClient(bool boolToSync)
    {
        if (boolToSync)
            syncForClient = true;
    }

    void MoveUsedCitizens()
    {
        GameObject yourCard = yPlayZone.GetChild(0).gameObject;
        CardModel cardModel = yourCard.GetComponent<CardModel>();
        cardModel.ToggleFace(true);
        if (yourCard.GetComponent<MoveCard>() != null)
        {
            MoveCard mv = yourCard.GetComponent<MoveCard>();
            Destroy(mv);
        }
        yourCard.transform.position = yDiscardZone.position;
        yourCard.transform.SetParent(yDiscardZone);

        GameObject enemyCard = ePlayZone.GetChild(0).gameObject;
        cardModel = enemyCard.GetComponent<CardModel>();
        cardModel.ToggleFace(true);
        if (enemyCard.GetComponent<MoveCard>() != null)
        {
            MoveCard mv = enemyCard.GetComponent<MoveCard>();
            Destroy(mv);
        }
        enemyCard.transform.position = eDiscardZone.position;
        enemyCard.transform.SetParent(eDiscardZone);
    }

    void FlipPlayedCards()
    {
        GameObject yourCard = yPlayZone.GetChild(0).gameObject;
        CardFlipper cardFlipper = yourCard.GetComponent<CardFlipper>();
        cardFlipper.FlipCard();
        GameObject enemyCard = ePlayZone.GetChild(0).gameObject;
        cardFlipper = enemyCard.GetComponent<CardFlipper>();
        cardFlipper.FlipCard();
    }

    [ClientRpc]
    void RpcTurnResult(string cMessage, int hScore, int cScore, bool ePlayed, bool sPlayed)
    {
        if (!isServer)
        {
            clientMessage = cMessage;
            hostScore = hScore;
            clientScore = cScore;
            emperorPlayed = ePlayed;
            slavePlayed = sPlayed;

            GameObject scoreObject = GameObject.Find("You").transform.GetChild(1).gameObject;
            Text scoreText = scoreObject.GetComponent<Text>();
            GameObject eScoreObject = GameObject.Find("Opponent").transform.GetChild(1).gameObject;
            Text eScoreText = eScoreObject.GetComponent<Text>();

            FlipPlayedCards();
            guidelines.text = clientMessage;
            scoreText.text = "Score: " + clientScore;
            eScoreText.text = "Score: " + hostScore;
        }
    }

    [Command]
    void CmdTurnResult()
    {
        GameObject scoreObject = GameObject.Find("You").transform.GetChild(1).gameObject;
        Text scoreText = scoreObject.GetComponent<Text>();
        GameObject eScoreObject = GameObject.Find("Opponent").transform.GetChild(1).gameObject;
        Text eScoreText = eScoreObject.GetComponent<Text>();

        if (yPlayZone.GetChild(0).name == "Citizen" && ePlayZone.GetChild(0).name == "Citizen")
        {
            message = "Your CITIZEN vs their CITIZEN. It's a DRAW.";
            clientMessage = "Your CITIZEN vs their CITIZEN. It's a DRAW.";
        }

        if (yPlayZone.GetChild(0).name == "Emperor" && ePlayZone.GetChild(0).name == "Citizen")
        {
            message = "Your EMPEROR vs their CITIZEN. You won this round.";
            clientMessage = "Your CITIZEN vs their EMPEROR. You lost this round.";
            emperorPlayed = true;
            hostScore++;
        }

        if (yPlayZone.GetChild(0).name == "Citizen" && ePlayZone.GetChild(0).name == "Emperor")
        {
            message = "Your CITIZEN vs their EMPEROR. You lost this round.";
            clientMessage = "Your EMPEROR vs their CITIZEN. You won this round.";
            emperorPlayed = true;
            clientScore++;
        }

        if (yPlayZone.GetChild(0).name == "Slave" && ePlayZone.GetChild(0).name == "Citizen")
        {
            message = "Your SLAVE vs their CITIZEN. You lost this round.";
            clientMessage = "Your CITIZEN vs their SLAVE. You won this round.";
            slavePlayed = true;
            clientScore++;
        }

        if (yPlayZone.GetChild(0).name == "Citizen" && ePlayZone.GetChild(0).name == "Slave")
        {
            message = "Your CITIZEN vs their SLAVE. You won this round.";
            clientMessage = "Your SLAVE vs their CITIZEN. You lost this round.";
            slavePlayed = true;
            hostScore++;
        }

        if (yPlayZone.GetChild(0).name == "Emperor" && ePlayZone.GetChild(0).name == "Slave")
        {
            message = "Your EMPEROR vs their SLAVE. You lost this round.";
            clientMessage = "Your SLAVE vs their EMPEROR. You won this round.";
            emperorPlayed = true;
            slavePlayed = true;
            clientScore += 5;
        }

        if (yPlayZone.GetChild(0).name == "Slave" && ePlayZone.GetChild(0).name == "Emperor")
        {
            message = "Your SLAVE vs their EMPEROR. You won this round.";
            clientMessage = "Your EMPEROR vs their SLAVE. You lost this round.";
            emperorPlayed = true;
            slavePlayed = true;
            hostScore += 5;
        }

        FlipPlayedCards();
        guidelines.text = message;
        scoreText.text = "Score: " + hostScore;
        eScoreText.text = "Score: " + clientScore;
        RpcTurnResult(clientMessage, hostScore, clientScore, emperorPlayed, slavePlayed);
        //TurnResultBools(emperorPlayed, slavePlayed);
    }

    [ClientRpc]
    void RpcAfterADraw()
    {
        if (!isServer)
            MoveUsedCitizens();
    }

    [Command]
    void CmdAfterADraw()
    {
        MoveUsedCitizens();
        RpcAfterADraw();
    }

    [ClientRpc]
    void RpcDestroyHands()
    {
        if (!isServer && hasAuthority)
        {
            if (yPlayZone.childCount > 0)
            {
                GameObject playedCard = yPlayZone.GetChild(0).gameObject;
                Destroy(playedCard);
            }

            if (ePlayZone.childCount > 0)
            {
                GameObject playedCard = ePlayZone.GetChild(0).gameObject;
                Destroy(playedCard);
            }

            for (int i = 0; i < yDiscardZone.childCount; i++)
            {
                GameObject discarded = yDiscardZone.GetChild(i).gameObject;
                Destroy(discarded);
            }

            for (int i = 0; i < eDiscardZone.childCount; i++)
            {
                GameObject discarded = eDiscardZone.GetChild(i).gameObject;
                Destroy(discarded);
            }
        }
    }

    [Command]
    void CmdDestroyHands()
    {
        if (yPlayZone.childCount > 0)
        {
            GameObject playedCard = yPlayZone.GetChild(0).gameObject;
            Destroy(playedCard);
        }

        if (ePlayZone.childCount > 0)
        {
            GameObject playedCard = ePlayZone.GetChild(0).gameObject;
            Destroy(playedCard);
        }

        GameObject eHand = GameObject.Find("EmperorHand(Clone)");
        Destroy(eHand);
        GameObject sHand = GameObject.Find("SlaveHand(Clone)");
        Destroy(sHand);

        for (int i = 0; i < yDiscardZone.childCount; i++)
        {
            GameObject discarded = yDiscardZone.GetChild(i).gameObject;
            Destroy(discarded);
        }

        for (int i = 0; i < eDiscardZone.childCount; i++)
        {
            GameObject discarded = eDiscardZone.GetChild(i).gameObject;
            Destroy(discarded);
        }

        RpcDestroyHands();
    }

    [ClientRpc]
    void RpcDisplayHandsAgain()
    {
        if (!isServer)
        {
            if (GameObject.Find("EmperorHand(Clone)") != null && GameObject.Find("SlaveHand(Clone)") != null)
            {
                Debug.Log("Found the hands. Trying to display hand again for the Client");

                GameObject empHand = Instantiate(emperorHand);
                empHand.transform.SetParent(gameCanvas);
                empHand.transform.localScale = new Vector3(1, 1, 1);
                RectTransform emperorRt = empHand.GetComponent<RectTransform>();

                GameObject slaHand = Instantiate(slaveHand);
                slaHand.transform.SetParent(gameCanvas);
                slaHand.transform.localScale = new Vector3(1, 1, 1);
                RectTransform slaveRt = slaHand.GetComponent<RectTransform>();

                if (hostTurn)
                {
                    slaveRt.anchoredPosition = new Vector3(-150, 275, 0);
                    slaHand.transform.SetParent(GameObject.Find("Opponent").transform);
                    emperorRt.anchoredPosition = new Vector3(-150, -275, 0);
                    empHand.transform.SetParent(GameObject.Find("You").transform);
                    clientMessage = "You are now the Emperor. It's your opponent's turn...";
                }

                else if (clientTurn)
                {
                    slaveRt.anchoredPosition = new Vector3(-150, -275, 0);
                    slaHand.transform.SetParent(GameObject.Find("You").transform);
                    emperorRt.anchoredPosition = new Vector3(-150, 275, 0);
                    empHand.transform.SetParent(GameObject.Find("Opponent").transform);
                    clientMessage = "You are now the Slave. Please play a card...";
                }

                guidelines.text = clientMessage;
            }

            else
                Debug.Log("Trying to display hands again for client. DID NOT FIND the hands.");
        }
    }

    [Command]
    void CmdDisplayHandsAgain()
    {
        GameObject eHand = Instantiate(emperorHand);
        //NetworkServer.Spawn(eHand);
        eHand.transform.SetParent(gameCanvas);
        eHand.transform.localScale = new Vector3(1, 1, 1);
        RectTransform emperorRt = eHand.GetComponent<RectTransform>();

        GameObject sHand = Instantiate(slaveHand);
        //NetworkServer.Spawn(sHand);
        sHand.transform.SetParent(gameCanvas);
        sHand.transform.localScale = new Vector3(1, 1, 1);
        RectTransform slaveRt = sHand.GetComponent<RectTransform>();

        if (hostTurn)
        {
            slaveRt.anchoredPosition = new Vector3(-150, -275, 0);
            sHand.transform.SetParent(GameObject.Find("You").transform);
            emperorRt.anchoredPosition = new Vector3(-150, 275, 0);
            eHand.transform.SetParent(GameObject.Find("Opponent").transform);
            message = "You are now the Slave. Please play a card...";
        }

        else if (clientTurn)
        {
            slaveRt.anchoredPosition = new Vector3(-150, 275, 0);
            sHand.transform.SetParent(GameObject.Find("Opponent").transform);
            emperorRt.anchoredPosition = new Vector3(-150, -275, 0);
            eHand.transform.SetParent(GameObject.Find("You").transform);
            message = "You are now the Emperor. It's your opponent's turn...";
        }

        guidelines.text = message;
        RpcDisplayHandsAgain();
    }

    [ClientRpc]
    void RpcResetBools()
    {
        hostPlayedOne = false;
        clientPlayedOne = false;
        syncForServer = false;
        syncForClient = false;
        whoPlayedOne = 0;
        hostCard = 0;
        clientCard = 0;
    }

    [Command]
    void CmdResetBools()
    {
        hostPlayedOne = false;
        clientPlayedOne = false;
        syncForServer = false;
        syncForClient = false;
        whoPlayedOne = 0;
        hostCard = 0;
        clientCard = 0;

        RpcResetBools();
    }
}
