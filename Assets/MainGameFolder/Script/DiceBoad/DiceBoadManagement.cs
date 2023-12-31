using UnityEngine;
using NextSceneScript;
using AllGameManager;
using System.Collections;

public class DiceBoadManagement : MonoBehaviour
{
    /// <summary> Mass status </summary>
    public enum CurrentMassStatus
    {
        Normal, Items, Heal, Enemy1, Enemy2, Boss,
    }
    /// <summary> Switch to next scene </summary>
    private NextScene chengeScene;
    /// <summary> Selected Weapons </summary>
    private WeponSellect wepon;
    /// <summary> All game status paramerter </summary>
    private AllGameStates playerStatus;
    /// <summary> Selected Enemys </summary>
    private EnemySellect enemy;
    /// <summary> All game sound effect manager </summary>
    private AllGameSEManager seManager;

    [Header("Charactor")]
    [SerializeField] Transform startPosition;
    [SerializeField] GameObject playerObject;
    [SerializeField] GameObject _player;
    [SerializeField] Transform player;
    [SerializeField] Animator playerAnimator;
    [SerializeField, Range(5f, 100f)] float moveSpeed = 5.0f;

    [Space, Header("Dice")]
    [SerializeField] GameObject[] DiceObject;
    [SerializeField] Transform DiceSpawnPosition;
    [SerializeField] GameObject _dice;
    [SerializeField] Dice diceScript;

    [Space, Header("MapEffect")]
    [SerializeField] GameObject healEffect;

    [Space, Header("ClearList")]
    [SerializeField] DB_ClearList clearList;

    //Private status
    private int movePoint;
    public bool playerTurn { get; private set; }
    private bool isStandby;
    private bool isMoving;
    private bool isMoved = true;
    public bool moveOut { get; private set; }
    private Vector3 nextPosition;
    private Quaternion nextQuaternion;
    private bool[] moveChecks = new bool[4];
    private GameObject[] Arrow = new GameObject[4];
    private Transform[] nextMass = new Transform[4];
    private CurrentMassStatus nowMass;
    private bool lateClear;
    public static bool[,] clearMass = new bool[11, 11];
    private int[] newWepons = new int[2];
    private bool isWeponSellect;
    private bool healing;
    private bool itemBoxOpening;
    static private bool enemyDown;

    public bool flont { get; private set; }
    public bool back { get; private set; }
    public bool right { get; private set; }
    public bool left { get; private set; }
    public bool jump { get; private set; }
    public bool run { get; private set; }
    public bool attack { get; private set; }
    public bool unique { get; private set; }
    public bool skill { get; private set; }

    void Awake()
    {
        GameObject _manager = GameObject.FindWithTag("GameManager");
        wepon = _manager.GetComponent<WeponSellect>();
        playerStatus = _manager.GetComponent<AllGameStates>();
        seManager = _manager.GetComponent<AllGameSEManager>();
        enemy = _manager.GetComponent<EnemySellect>();
        chengeScene = GetComponent<NextScene>();
        Cursor.lockState = CursorLockMode.None;
        isMoving = false;
        isMoved = true;
        if (playerStatus.GetStart())
        {
            _player = Instantiate(playerObject, playerStatus.GetLatePosition(), playerStatus.GetLateQuaternion());
        }
        else
        {
            healing = false;
            itemBoxOpening = false;
            enemyDown = false;
            playerStatus.ResetHP();
            playerStatus.AddPlayerLateHP();
            _player = Instantiate(playerObject, startPosition);
            playerStatus.AddStart();
        }
        playerAnimator = _player.GetComponentInChildren<Animator>();
        nextPosition = _player.transform.position;
        lateClear = playerStatus.GetLateClear();
        int arrow = 0;
        while (arrow < 4)
        {
            switch (arrow)
            {
                case 0:
                    Arrow[arrow] = GameObject.Find("Arrow_F"); break;
                case 1:
                    Arrow[arrow] = GameObject.Find("Arrow_B"); break;
                case 2:
                    Arrow[arrow] = GameObject.Find("Arrow_R"); break;
                case 3:
                    Arrow[arrow] = GameObject.Find("Arrow_L"); break;
            }
            arrow++;
        }
    }

    void Update()
    {
        MassMove();
        GamePlay();
    }

    void MassMove()
    {
        switch (nowMass)
        {
            //Normal
            case CurrentMassStatus.Normal:
                if (!isMoving & moveOut)
                {
                    AddSerialBools();
                    isMoved = true;
                }
                break;
            //Items
            case CurrentMassStatus.Items:
                if (!isMoving & moveOut)
                {
                    AddSerialBools();
                    itemBoxOpening = true;
                    if (seManager.DB_MassSE[1] != null & !seManager.DB_MassSESource.isPlaying)
                        seManager.DB_MassSESource.PlayOneShot(seManager.DB_MassSE[1]);
                    isMoved = true;
                }
                break;
            //Heal
            case CurrentMassStatus.Heal:
                if (!isMoving & !isMoved & moveOut)
                {
                    AddSerialBools();
                    healing = true;
                    playerStatus.AddPlayerLateHP();
                    playerStatus.AddHealHP(999);
                    Instantiate(healEffect, player);
                    if (seManager.DB_MassSE[2] != null) seManager.DB_MassSESource.PlayOneShot(seManager.DB_MassSE[2]);
                    isMoved = true;
                }
                break;
            //Enemy1
            case CurrentMassStatus.Enemy1:
                if (!isMoving & !isMoved & moveOut)
                {
                    AddSerialBools();
                    enemyDown = true;
                    enemy.AddLevel(1);
                    SceneChenge("BattleScene");
                }
                break;
            //Enemy2
            case CurrentMassStatus.Enemy2:
                if (!isMoving & !isMoved & moveOut)
                {
                    AddSerialBools();
                    enemyDown = true;
                    enemy.AddLevel(2);
                    SceneChenge("BattleScene");
                }
                break;
            //Boss
            case CurrentMassStatus.Boss:
                enemy.AddLevel(3);
                SceneChenge("BattleScene");
                break;
        }
    }

    void GamePlay()
    {
        Manager();
        if (playerTurn)
        {
            if (skill & _dice == null)
            {
                playerStatus.AddPlayTurn();
                int random = Random.Range(0, 5);
                _dice = Instantiate(DiceObject[random], DiceSpawnPosition.position, DiceObject[random].transform.rotation);
                diceScript = _dice.GetComponentInChildren<Dice>();
                if (seManager.DB_AnySE[1] != null & seManager.DB_MassSESource != null) seManager.DB_MassSESource.PlayOneShot(seManager.DB_AnySE[1]);
            }
            if (diceScript != null)
            {
                if (diceScript.GetIsStoping())
                {
                    movePoint = diceScript.GetResult();
                    isMoved = false;
                    lateClear = false;
                    Destroy(_dice);
                    StartCoroutine(Move());
                }
            }
        }
        MoveAnim();
    }

    private IEnumerator Move()
    {
        isMoving = true;
        while (movePoint > 0)
        {
            PlayerMove();
            yield return null;
        }
        isMoving = false;
    }

    void PlayerMove()
    {
        if (movePoint > 0 & moveOut)
        {
            if (flont & moveChecks[0])
            {
                nextPosition = nextMass[0].position;
                nextQuaternion = Quaternion.AngleAxis(0, Vector3.up);
                player.localRotation *= nextQuaternion;
                int randomNum = Random.Range(0, seManager.DB_FootSE.Length);
                if (seManager.DB_FootSE[randomNum] != null) seManager.DB_FootSESource.PlayOneShot(seManager.DB_FootSE[randomNum]);
                movePoint--;
                return;
            }
            if (back & moveChecks[1])
            {
                nextPosition = nextMass[1].position;
                nextQuaternion = Quaternion.AngleAxis(180, Vector3.up);
                player.localRotation *= nextQuaternion;
                int randomNum = Random.Range(0, seManager.DB_FootSE.Length);
                if (seManager.DB_FootSE[randomNum] != null) seManager.DB_FootSESource.PlayOneShot(seManager.DB_FootSE[randomNum]);
                movePoint--;
                return;
            }
            if (right & moveChecks[2])
            {
                nextPosition = nextMass[2].position;
                nextQuaternion = Quaternion.AngleAxis(90, Vector3.up);
                player.localRotation *= nextQuaternion;
                int randomNum = Random.Range(0, seManager.DB_FootSE.Length);
                if (seManager.DB_FootSE[randomNum] != null) seManager.DB_FootSESource.PlayOneShot(seManager.DB_FootSE[randomNum]);
                movePoint--;
                return;
            }
            if (left & moveChecks[3])
            {
                nextPosition = nextMass[3].position;
                nextQuaternion = Quaternion.AngleAxis(-90, Vector3.up);
                player.localRotation *= nextQuaternion;
                int randomNum = Random.Range(0, seManager.DB_FootSE.Length);
                if (seManager.DB_FootSE[randomNum] != null) seManager.DB_FootSESource.PlayOneShot(seManager.DB_FootSE[randomNum]);
                movePoint--;
                return;
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void MoveAnim()
    {
        moveOut = player.position == nextPosition;
        playerAnimator.SetBool("Moving", !moveOut);
        player.position = Vector3.MoveTowards(player.position, nextPosition, moveSpeed * Time.deltaTime);
    }

    /// <summary>
    /// 
    /// </summary>
    void Manager()
    {
        KeyInput();
        playerTurn = !isMoving & !healing & !isWeponSellect & !itemBoxOpening & !enemyDown;
        player = _player.transform;
        isStandby = !isMoving & _dice == null;

        int arrow = 0;
        while (arrow < 4)
        {
            Arrow[arrow].SetActive(moveChecks[arrow] & isMoving);
            arrow++;
        }
    }

    void KeyInput()
    {
        flont = Input.GetKeyDown(Controller.Flont);
        back = Input.GetKeyDown(Controller.Back);
        right = Input.GetKeyDown(Controller.Right);
        left = Input.GetKeyDown(Controller.Left);
        jump = Input.GetKeyDown(Controller.Jump);
        run = Input.GetKeyDown(Controller.Run);
        attack = Input.GetKeyDown(Controller.Attack);
        unique = Input.GetKeyDown(Controller.Unique);
        skill = Input.GetKeyDown(Controller.Skill);
    }

    void Debugger()
    {
        int num1 = 0, num2;
        while (num1 < 11)
        {
            num2 = 0;
            while (num2 < 11)
            {
                if (clearMass[num1, num2])
                {
                    Debug.LogWarning("(" + num1 + "," + num2 + ") = true");
                }
                num2++;
            }
            num1++;
        }
    }

    void AddSerialBools()
    {
        int num1 = 0;
        int num2;
        while (num1 < 11)
        {
            num2 = 0;
            while (num2 < 11)
            {
                clearList.GetBools(num1, num2, clearMass[num1, num2]);
                num2++;
            }
            num1++;
        }
    }

    void SceneChenge(string sceneName)
    {
        playerStatus.AddPosition(player.position);
        playerStatus.AddQuaternion(player.rotation);
        chengeScene.ChengeScene(sceneName);
    }

    public void AddEventMass() { playerStatus.AddEventMass(); }

    public int GetMovePoint() { return movePoint; }
    public bool GetLateClaer() { return lateClear; }
    public bool GetIsStandby() { return isStandby; }
    public bool GetIsMoved() { return isMoved; }
    public bool GetClearMass(int num1, int num2) { return clearMass[num1, num2]; }
    public int GetNewWepons(int SelORLev) { return newWepons[SelORLev]; }
    public bool GetIsWeponSellect() { return isWeponSellect; }

    public void SetMoveCheck(int num, bool check) { moveChecks[num] = check; }
    public void SetNextMass(int num, Transform next) { nextMass[num] = next; }
    public void SetLateClear(bool late) { lateClear = late; }
    public void SetHealing(bool heal) { healing = heal; }
    public void SetEnemyDown(bool down) { enemyDown = down; }
    public void SetClearMass(int num1, int num2, bool isClear) { clearMass[num1, num2] = isClear; }
    public void SetNewWepons(int weponSellection, int Level) { newWepons[0] = weponSellection; newWepons[1] = Level; isWeponSellect = true; }

    public void CurrentMass(int massStatus)
    {
        switch (massStatus)
        {
            case 0: nowMass = CurrentMassStatus.Normal; break; //Normal
            case 1: nowMass = CurrentMassStatus.Items; break; //Items
            case 2: nowMass = CurrentMassStatus.Heal; break; //Heal
            case 3: nowMass = CurrentMassStatus.Enemy1; break; //Enemy1
            case 4: nowMass = CurrentMassStatus.Enemy2; break; //Enemy2
            case 5: nowMass = CurrentMassStatus.Boss; break; //Boss
        }
    }

    public void OnWeponSellectedBotton(bool chenge)
    {
        if (chenge)
        {
            switch (newWepons[0])
            {
                case 0: wepon.wepon = WeponSellect.Wepon.Sword; break;
                case 1: wepon.wepon = WeponSellect.Wepon.Spear; break;
                case 2: wepon.wepon = WeponSellect.Wepon.Bow; break;
                case 3: wepon.wepon = WeponSellect.Wepon.Gun; break;
                case 4: wepon.wepon = WeponSellect.Wepon.Magic; break;
            }
            switch (newWepons[1])
            {
                case 0: wepon.rarelity = WeponSellect.Rarelity.Common; break;
                case 1: wepon.rarelity = WeponSellect.Rarelity.Rare; break;
                case 2: wepon.rarelity = WeponSellect.Rarelity.Unique; break;
            }
            playerStatus.AddHealHP(0);
        }
        isWeponSellect = false;
        itemBoxOpening = false;
    }
}
