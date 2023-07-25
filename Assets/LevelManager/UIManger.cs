using System.Collections.Generic;
using Chainstrike.Types;
using codebase.utility;
using MoreMountains.TopDownEngine;
using Solana.Unity.SDK;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Facing = Chainstrike.Types.Facing;

// ReSharper disable once CheckNamespace

public class UIManger : MonoBehaviourSingleton<UIManger>
{
    [SerializeField]
    private GameObject blockPrefab;
    
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
    private GameObject explosionPrefab;

    private LevelManager levelManager;
    private GameObject healthBar;
    private CharacterGridMovement gridCharacter;
    private InputManager inputManager;
    GameObject[,] instantiatedBlocks = new GameObject[30, 30];
    GameObject[,] instantiatedRecharger = new GameObject[30, 30];
    
    private IDictionary<string, GameObject> _enemies = new Dictionary<string, GameObject>();

    private void Start()
    {
        levelManager = GameObject.Find("LevelManager").GetComponent<LevelManager>();
        healthBar = GameObject.Find("HealthBarFront");
        gridCharacter = GameObject.Find("MinimalGridCharacter").GetComponent<CharacterGridMovement>();
        inputManager = GameObject.Find("UICamera").GetComponent<InputManager>();
        
        txtGameId.text = PlayerPrefs.GetString("gameID", "");;
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
        var explosion = Instantiate(explosionPrefab, gridCharacter.transform.position, Quaternion.identity);
        Destroy(explosion, 5f);
    }

    public void StartReceivingInput()
    {
        //inputManager.InputDetectionActive = true;
        gridCharacter.DisableInput = false;
    }
    
    public void StopReceivingInput()
    {
        // gridCharacter.StopDetection();
        // inputManager.InputDetectionActive = false;
        // gridCharacter.StopDetection();
        gridCharacter.DisableInput = true;
    }
    
    private void CreateBlock(int x, int y)
    {
        if (instantiatedBlocks[UnMapX(x), UnMapY(y)] != null) return;
        Vector3 position = new Vector3(x, 0, y);
        var block = Instantiate(blockPrefab, position, Quaternion.identity, grid.transform);
        instantiatedBlocks[UnMapX(x), UnMapY(y)] = block;
    }

    private void CreateRecharge(int x, int y)
    {
        if (instantiatedRecharger[UnMapX(x), UnMapY(y)] != null) return;
        Vector3 position = new Vector3(x, 0, y);
        var block = Instantiate(rechargerPrefab, position, Quaternion.identity, grid.transform);
        instantiatedRecharger[UnMapX(x), UnMapY(y)] = rechargerPrefab;
    }

    public void SetGrid(Cell[][] gridCells)
    {
        for (var y = 0; y < gridCells.Length; y++)
        {
            for (var x = 0; x < gridCells[y].Length; x++)
            {
                if (gridCells[x][y] == Cell.Block) CreateBlock(MapX(x), MapY(y));
                if (gridCells[x][y] == Cell.Recharge) CreateRecharge(MapX(x), MapY(y));
            }
        }
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
            _enemies.Add(player.Address, enemy);
        }
        else
        {
            enemy.transform.position = playerPos;
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
    
    public void SetCharacters(Player[] gamePlayers)
    {
        if(Web3.Account == null) return;
        foreach (var player in gamePlayers)
        {
            if (player.Address.Equals(Web3.Account.PublicKey))
            {
                Debug.Log("Set current player");

                var playerObject = levelManager.Players.ToArray()[0];
                if (playerObject.MovementState.CurrentState == CharacterStates.MovementStates.Idle)
                {
                    var playerPos = GridManager.Instance.CellToWorldCoordinates(new Vector3Int(MapPlayerX(player.X), 0, MapPlayerY(player.Y)));
                    playerPos.y = -0.5f;
                    if(Vector3.Distance(playerObject.transform.position, playerPos) < 0.1f)
                    {
                        playerObject.transform.position = playerPos;
                    }
                    else
                    {
                        playerObject.transform.position = playerPos;
                        playerObject.RespawnAt(playerObject.transform, MapFacing(player.Facing));
                        gridCharacter.Stop(MapFacingToGrid(player.Facing));
                        gridCharacter.StopMovement();
                        gridCharacter.SetCurrentWorldPositionAsNewPosition();
                    }
                }

                healthBar.transform.localScale = new Vector3(player.Energy/100f, 1, 1);
                if (player.Energy == 0)
                {
                    StopReceivingInput();
                    deathScreen.SetActive(true);
                    txtInfo.text = "You died! :( ";
                }
                else
                {
                    txtInfo.text = "";
                    deathScreen.SetActive(false);
                }
            }
            else
            {
                Debug.Log("Set other players");
                InstantiatePlayer(player);
            }
        }
    }

    private int MapX(int x)
    {
        return x - 14;
    }

    private int MapY(int y)
    {
        return y - 13;
    }
    
    private int UnMapX(int x)
    {
        return x + 14;
    }

    private int UnMapY(int y)
    {
        return y + 13;
    }
    
    private int MapPlayerX(int x)
    {
        return x - 14;
    }

    private int MapPlayerY(int y)
    {
        return y - 14;
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
        PlayerPrefs.SetString("gameID", gameID);
    }
    
    public string GetGameID()
    {
        return txtGameId.text.Replace("\u200B", "");
    }
}
