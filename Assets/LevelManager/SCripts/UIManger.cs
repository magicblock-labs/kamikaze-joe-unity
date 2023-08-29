using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Chainstrike;
using KamikazeJoe.Types;
using codebase.utility;
using Cysharp.Threading.Tasks;
using MoreMountains.TopDownEngine;
using Solana.Unity.SDK;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Facing = KamikazeJoe.Types.Facing;

// ReSharper disable once CheckNamespace

public class UIManger : MonoBehaviourSingleton<UIManger>
{
    [SerializeField]
    private GameObject blockPrefab;
    
    [SerializeField]
    private GameObject leftBorderPrefab;
    
    [SerializeField]
    private GameObject rightBorderPrefab;
    
    [SerializeField]
    private GameObject topBorderPrefab;
    
    [SerializeField]
    private GameObject bottomBorderPrefab;
    
    
    [SerializeField]
    private GameObject rechargerPrefab;
    
    [SerializeField]
    private GameObject enemyPrefab;
    
    [SerializeField]
    private Text txtInfo;
    
    [SerializeField]
    private GameObject grid;
    
    [SerializeField]
    private GameObject deathScreen;
    
    [SerializeField]
    private TMP_InputField txtGameId;
    
    [SerializeField]
    private Text txtPricePool;
    
    [SerializeField]
    private GameObject explosionPrefab;
    
    [SerializeField]
    private GameObject wonImage;
    
    [SerializeField]
    private Material[] enemyMaterials;

    private LevelManager _levelManager;
    private GameObject _healthBar;
    private CharacterGridMovement _gridCharacter;
    private List<GameObject> _instantiatedBlocks = new();
    private List<GameObject> _instantiatedRecharger = new();

    private readonly IDictionary<string, GameObject> _enemies = new Dictionary<string, GameObject>();
    private int _gridWidth = 28;
    private int _gridHeight = 28;

    private void Start()
    {
        _levelManager = GameObject.Find("LevelManager").GetComponent<LevelManager>();
        _healthBar = GameObject.Find("HealthBarFront");
        _gridCharacter = GameObject.Find("MinimalGridCharacter").GetComponent<CharacterGridMovement>();
        txtGameId.text = PlayerPrefs.GetString("gameID", "");
    }

    private void OnEnable()
    {
        DetectEnergyChange.OnExplosion += OnExplosionHandler;
    }

    private void OnDisable()
    {
        DetectEnergyChange.OnExplosion -= OnExplosionHandler;
    }

    private void OnExplosionHandler()
    {
        var explosion = Instantiate(explosionPrefab, _gridCharacter.transform.position, Quaternion.identity);
        Destroy(explosion, 5f);
    }

    public void StartReceivingInput()
    {
        if(_gridCharacter != null) _gridCharacter.DisableInput = false;
    }
    
    public void StopReceivingInput()
    {
        if(_gridCharacter != null) _gridCharacter.DisableInput = true;
    }

    public void ResetEnergy()
    {
        _gridCharacter.ResetEnergy();
    }
    
    public void ResetLevel()
    {
        _gridCharacter.DisableInput = true;
        _gridCharacter.StopAllMovements();
        foreach (var enemiesValue in _enemies.Values)
        {
            Destroy(enemiesValue);
        }
        _enemies.Clear();
        _instantiatedBlocks.ForEach(Destroy);
        _instantiatedBlocks.Clear();
        _instantiatedBlocks = null;
        _instantiatedRecharger.ForEach(Destroy);
        _instantiatedRecharger.Clear();
        _instantiatedRecharger = null;
    }
    
    private void CreateBlock(int x, int y)
    {
        Vector3 position = new Vector3(x, 0, y);
        var block = Instantiate(blockPrefab, position, Quaternion.identity, grid.transform);
        _instantiatedBlocks.Add(block);
    }

    private void CreateRecharge(int x, int y)
    {
        Vector3 position = new Vector3(x, 0, y);
        var recharger = Instantiate(rechargerPrefab, position, Quaternion.identity, grid.transform);
        _instantiatedRecharger.Add(recharger);
    }

    public void SetGrid(Cell[][] gridCells)
    {
        // Create the environment
        CreateEnvironment(gridCells);

        // Adapt the border
        CreateBorders(gridCells.Length, gridCells[0].Length);
    }

    private void CreateEnvironment(Cell[][] gridCells)
    {
        if (_instantiatedBlocks != null && _instantiatedRecharger != null) return;
        _instantiatedBlocks = new List<GameObject>();
        _instantiatedRecharger = new List<GameObject>();
        _gridWidth = gridCells.Length;
        _gridHeight = gridCells[0].Length;
        for (var y = 0; y < gridCells.Length; y++)
        {
            for (var x = 0; x < gridCells[y].Length; x++)
            {
                if (gridCells[x][y] == Cell.Block) CreateBlock(MapX(x), MapY(y));
                if (gridCells[x][y] == Cell.Recharge) CreateRecharge(MapX(x), MapY(y));
            }
        }
    }

    private void CreateBorders(int width, int height)
    {
        leftBorderPrefab.transform.localScale = new Vector3(1.0f, 1.01f, height/31.1f);
        var span = width/2 + 1;
        leftBorderPrefab.transform.position = new Vector3(-span, -0.5f, span);
        
        rightBorderPrefab.transform.localScale = new Vector3(1.0f, 1.01f, height/31.1f);
        span = width/2 + 1;
        rightBorderPrefab.transform.position = new Vector3(span, -0.5f, -span);
        
        bottomBorderPrefab.transform.localScale = new Vector3(1.0f, 1.0f, height/31.1f);
        span = width/2 + 1;
        bottomBorderPrefab.transform.position = new Vector3(-span, -0.5f, -span);
        
        topBorderPrefab.transform.localScale = new Vector3(1.0f, 1.0f, height/31.1f);
        span = width/2 + 1;
        topBorderPrefab.transform.position = new Vector3(span, -0.5f, span);

        _gridCharacter.width = MapX(width);
        _gridCharacter.height = MapY(height);
    }

    private void InstantiatePlayer(Player player)
    {
        var playerPos = GridManager.Instance.CellToWorldCoordinates(new Vector3Int(MapPlayerX(player.X), 0, MapPlayerY(player.Y)));
        playerPos.y = -0.5f;
        _enemies.TryGetValue(player.Address, out var enemy);
        if (player.Energy == 0)
        {
            if(enemy) Destroy(enemy);
            return;
        }
        if(enemy == null)
        {
            enemy = Instantiate(enemyPrefab, playerPos, Quaternion.identity);
            var randomMaterial = enemyMaterials[player.Address.KeyBytes[0] % enemyMaterials.Length];
            enemy.GetComponentInChildren<MeshRenderer>().materials[0] = randomMaterial;
            enemy.GetComponentInChildren<MeshRenderer>().material = randomMaterial;
            var enemyInfo = enemy.AddComponent<Enemy>();
            enemyInfo.energy = player.Energy;
            _enemies.Add(player.Address, enemy);
        }
        else
        {
            StartCoroutine(MoveToSpot(enemy.transform, playerPos, 0.2f));
            var enemyInfo = enemy.GetComponent<Enemy>();
            if (enemyInfo.energy - player.Energy > 15)
            {
                var explosion = Instantiate(explosionPrefab, playerPos, Quaternion.identity);
                Destroy(explosion, 5f);
            }
            enemyInfo.energy = player.Energy;
        }

        switch (player.Facing)
        {
            case Facing.Up:
                enemy.transform.rotation = Quaternion.Euler(0, 0, 0);
                break;
            case Facing.Down:
                enemy.transform.rotation = Quaternion.Euler(0, 180, 0);
                break;
            case Facing.Right:
                enemy.transform.rotation = Quaternion.Euler(0, 90, 0);
                break;
            case Facing.Left:
                enemy.transform.rotation = Quaternion.Euler(0, 270, 0);
                break;
        }
    }
    
    public async UniTask WaitCharacterIdle()
    {
        await UniTask.WaitUntil( () => _levelManager.Players.ToArray()[0].MovementState.CurrentState == CharacterStates.MovementStates.Idle);
    }
    
    public void SetCharacters(Player[] gamePlayers)
    {
        if(Web3.Account == null) return;
        foreach (var player in gamePlayers)
        {
            if (player.Address.Equals(Web3.Account.PublicKey))
            {
                var playerObject = _levelManager.Players.ToArray()[0];
                if (playerObject.MovementState.CurrentState == CharacterStates.MovementStates.Idle)
                {
                    var playerPos = GridManager.Instance.CellToWorldCoordinates(new Vector3Int(MapPlayerX(player.X), 0, MapPlayerY(player.Y)));
                    playerPos.y = -0.5f;
                    playerObject.transform.position = playerPos;
                    _gridCharacter.Stop(MapFacingToGrid(player.Facing));
                    _gridCharacter.StopMovement();
                    _gridCharacter.SetCurrentWorldPositionAsNewPosition();
                }

                _healthBar.transform.localScale = new Vector3(player.Energy/100f, 1, 1);
                if (player.Energy == 0)
                {
                    StopReceivingInput();
                    deathScreen.SetActive(true);
                    txtInfo.text = "You died! :( ";
                    if (wonImage != null) wonImage.SetActive(false);
                }
                else
                {
                    txtInfo.text = "";
                    deathScreen.SetActive(false);
                    if (wonImage != null) wonImage.SetActive(false);
                }
            }
            else
            {
                InstantiatePlayer(player);
            }
        }
    }
    
    public void ShowWinningScreen()
    {
        deathScreen.SetActive(true);
        txtInfo.text = "You WON! :D";
        if (wonImage != null) wonImage.SetActive(true);
        StopReceivingInput();
    }

    private int MapX(int x)
    {
        return x - _gridWidth/2;
    }

    private int MapY(int y)
    {
        return y - (_gridHeight/2 - 1);
    }
    
    private int UnMapX(int x)
    {
        return x + _gridWidth/2;
    }

    private int UnMapY(int y)
    {
        return y + (_gridHeight/2 - 1);
    }
    
    private int MapPlayerX(int x)
    {
        return x - _gridWidth/2;
    }

    private int MapPlayerY(int y)
    {
        return y - _gridWidth/2;;
    }

    public static Character.FacingDirections MapFacing(Facing facing)
    {
        Character.FacingDirections dir = Character.FacingDirections.South;
        switch(facing) 
        {
            case Facing.Up:
                dir = Character.FacingDirections.North;
                break;
            case Facing.Down:
                dir = Character.FacingDirections.South;
                break;
            case Facing.Right:
                dir = Character.FacingDirections.East;
                break;
            case Facing.Left:
                dir = Character.FacingDirections.West;
                break;
        }

        return dir;
    }
    
    public static CharacterGridMovement.GridDirections MapFacingToGrid(Facing facing)
    {
        CharacterGridMovement.GridDirections dir = CharacterGridMovement.GridDirections.Down;
        switch(facing) 
        {
            case Facing.Up:
                dir = CharacterGridMovement.GridDirections.Up;
                break;
            case Facing.Down:
                dir = CharacterGridMovement.GridDirections.Down;
                break;
            case Facing.Right:
                dir = CharacterGridMovement.GridDirections.Right;
                break;
            case Facing.Left:
                dir = CharacterGridMovement.GridDirections.Left;
                break;
        }

        return dir;
    }
    
    public static Facing UnMapFacing(CharacterGridMovement.GridDirections facing)
    {
        Facing dir = Facing.Down;
        switch(facing) 
        {
            case CharacterGridMovement.GridDirections.Up:
                dir = Facing.Up;
                break;
            case CharacterGridMovement.GridDirections.Down:
                dir = Facing.Down;
                break;
            case CharacterGridMovement.GridDirections.Right:
                dir = Facing.Right;
                break;
            case CharacterGridMovement.GridDirections.Left:
                dir = Facing.Left;
                break;
        }

        return dir;
    }
    
    private IEnumerator MoveToSpot(Transform targetTransform, Vector3 toPosition, float waitTime = 0.3f)
    {
        float elapsedTime = 0;
        var currentPos = targetTransform.position;
        while (elapsedTime < waitTime)
        {
            targetTransform.position = Vector3.Lerp(currentPos, toPosition, (elapsedTime / waitTime));
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        targetTransform.position = toPosition;
        yield return null;
    }


    public void ToogleMenu()
    {
        deathScreen.SetActive(!deathScreen.gameObject.activeSelf);
    }
    
    public void CopyPublicKey()
    {
        if(Web3.Account == null) return;
        Clipboard.Copy(Web3.Account.PublicKey);
        Debug.Log("Public Key copied: " + Web3.Account.PublicKey);
    }

    public void SetGameID(string gameID)
    {
        txtGameId.text = gameID;
        PlayerPrefs.SetString("pkPlayer", Web3.Account.PublicKey);
        PlayerPrefs.SetString("gameID", gameID);
    }
    
    public void SetPrizePool(float prizePool)
    {
        if (prizePool > 0)
        {
            txtPricePool.gameObject.SetActive(true);
            txtPricePool.text = "Prize: " + prizePool.ToString(CultureInfo.InvariantCulture);
        }
        else
        {
            txtPricePool.gameObject.SetActive(false);
        }
    }
    
    public string GetGameID()
    {
        return txtGameId.text.Replace("\u200B", "");
    }
}
