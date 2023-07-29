using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chainstrike;
using Chainstrike.Accounts;
using Chainstrike.Program;
using Chainstrike.Types;
using Cysharp.Threading.Tasks;
using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using Solana.Unity.Programs;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Core.Sockets;
using Solana.Unity.Rpc.Messages;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Rpc.Types;
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using UnityEngine;
using UnityEngine.UI;
using Random = System.Random;

// ReSharper disable once CheckNamespace

public class ChainStrikeClient : MonoBehaviour
{
    [SerializeField]
    private Button joinGameBtn;
    
    [SerializeField]
    private Button newGameBtn;
    
    [SerializeField]
    private Button joinRandomArenaBtn;
    
    [SerializeField]
    private MMTouchButton publicCheckBox;

    private Toast _toast;
    private PublicKey _gameInstanceId;
    
    private readonly PublicKey _chainStrikeProgramId = new("JoeXD3mj5VXB2xKUz6jJ8D2AC72pXCydA6fnQJg2JiG");
    //private readonly PublicKey _chainStrikeProgramId = new("3ARzo1BnheocchBNMxEo5f86nZ7MkyyiSgZGBn6WBxPf");
    
    private ChainstrikeClient _chainstrikeClient;
    private ChainstrikeClient ChainstrikeClient => _chainstrikeClient ??= 
        new ChainstrikeClient(Web3.Rpc, Web3.WsRpc, _chainStrikeProgramId);

    private static readonly int[][] spawnPoints = new int[][] { new[] { 1, 1}, new[] { 26, 26}, new[] { 1, 26}, new[] { 26, 1} };
    private bool _isMoving;
    private bool _initPlayer = true;
    private Facing _prevMove;

    private void OnEnable()
    {
        CharacterGridMovement.OnGridMovementEvent += OnMove;
        DetectEnergyChange.OnExplosion += OnExplosion;
        Web3.OnLogin += OnLogin;
    }

    private void OnDisable()
    {
        CharacterGridMovement.OnGridMovementEvent -= OnMove;
        DetectEnergyChange.OnExplosion -= OnExplosion;
        Web3.OnLogin -= OnLogin;
    }

    private void OnLogin(Account account)
    {
        var prevMatch = PlayerPrefs.GetString("gameID", null);
        if (prevMatch != null && account.PublicKey.ToString().Equals(PlayerPrefs.GetString("pkPlayer", null)))
        {
            JoinGame(prevMatch).Forget();
        }
        else
        {
            UIManger.Instance.ToogleMenu();
        }
    }


    private void OnMove(CharacterGridMovement.GridDirections direction)
    {
        if(Web3.Account == null) return;
        if(_gameInstanceId == null) return;
        if(direction == CharacterGridMovement.GridDirections.None) return;
        MakeMove(UIManger.UnMapFacing(direction), CharacterGridMovement.EnergyToUse).Forget();
    }
    
    private void OnExplosion()
    {
        if(Web3.Account == null) return;
        if(_gameInstanceId == null) return;
        MakeExplosion().Forget();
    }

    void Start()
    {
        if (newGameBtn != null) newGameBtn.onClick.AddListener(CallCreateGame);
        if (joinGameBtn != null) joinGameBtn.onClick.AddListener(CallJoinGame);
        if (joinRandomArenaBtn != null) joinRandomArenaBtn.onClick.AddListener(CallJoinRandomArena);
        _toast = GetComponent<Toast>();
    }
    
    private void CallCreateGame()
    {
        CreateGame().Forget();
    }
    
    private void CallJoinGame()
    {
        var gameId = UIManger.Instance.GetGameID();
        JoinGame(gameId).Forget();
    }
    
    private void CallJoinRandomArena()
    {
        JoinRandomArena().Forget();
    }

    private async UniTask ReloadGame()
    {
        var game = (await ChainstrikeClient.GetGameAsync(_gameInstanceId, Commitment.Confirmed)).ParsedResult;
        SetGame(game);
    }

    private async UniTask JoinGame(string gameId)
    {
        if(Web3.Account == null) return;
        Loading.StartLoading();
        var game = (await ChainstrikeClient.GetGameAsync(gameId, Commitment.Confirmed)).ParsedResult;
        if(game == null) return;

        var mustJoin = !game.Players.Select(p => p.Address).Contains(Web3.Account);
        if (mustJoin)
        {
            var res = await CreateGameTransaction(gameId);
            Debug.Log($"Signature: {res.Result}");
            if (res.WasSuccessful)
            {
                await Web3.Rpc.ConfirmTransaction(res.Result, Commitment.Confirmed);
            }
            Debug.Log("Joined Game");
        }
        game = (await ChainstrikeClient.GetGameAsync(gameId, Commitment.Confirmed)).ParsedResult;
        Loading.StopLoading();
        _gameInstanceId = new PublicKey(gameId);
        UIManger.Instance.SetGameID(_gameInstanceId);
        _initPlayer = true;
        SetGame(game);
        Debug.Log($"Subscribing to game");
        SubscribeToGame(new PublicKey(gameId)).Forget();
        Debug.Log($"Game Id: {gameId}");
    }
    
    private async UniTask CreateGame()
    {
        if(Web3.Account == null) return;
        Loading.StartLoading();
        var userPda = FindUserPda(Web3.Account);
        User userAccount = null;
        if (await IsPdaInitialized(userPda))
        {
            userAccount = (await ChainstrikeClient.GetUserAsync(userPda)).ParsedResult;
        }
        var gamePdaIdx = userAccount == null ? 0 : userAccount.Games;
        PublicKey gamePda = null;
        Debug.Log($"Searching game PDA");
        while (gamePda == null)
        {
            var gameTempPda = FindGamePda(userPda, gamePdaIdx);
            if(!await IsPdaInitialized(gameTempPda)) gamePda = gameTempPda;
            gamePdaIdx++;
        }
        Debug.Log($"Sending transaction new Game");
        var res = await CreateGameTransaction(gamePda, publicMatch: publicCheckBox == null || publicCheckBox.GetComponent<Image>().sprite.name.Equals("CheckboxOff"));
        Debug.Log($"Signature: {res.Result}");
        if (res.WasSuccessful)
        {
            await Web3.Rpc.ConfirmTransaction(res.Result, Commitment.Confirmed);
            
            Game game = null;
            var retry = 5;
            while (game == null && retry > 0)
            {
                game = (await ChainstrikeClient.GetGameAsync(gamePda, Commitment.Confirmed)).ParsedResult;
                retry--;
                await UniTask.Delay(TimeSpan.FromSeconds(1));
            }
            Debug.Log($"Game retrieved");
            _gameInstanceId = gamePda;
            UIManger.Instance.SetGameID(_gameInstanceId);
            Debug.Log($"Setting game");
            _initPlayer = true;
            SetGame(game);
            Debug.Log($"Subcribing to game");
            SubscribeToGame(gamePda).Forget();
            Debug.Log($"Game Id: {gamePda}");
        }
        Loading.StopLoading();
        UIManger.Instance.StartReceivingInput();
    }
    
    private async UniTask JoinRandomArena()
    {
        if(Web3.Account == null) return;
        Loading.StartLoading();
        var gameToJoin = await FindGameToJoin();
        if (gameToJoin != null)
        {
            await JoinGame(gameToJoin);
        }
        if(gameToJoin == null) Debug.Log("Unable to find a game to join");
        Loading.StopLoading();
    }
    
    private async UniTask MakeMove(Facing facing, int energy, int retry = 5)
    {
        if(Web3.Account == null) return;
        if(_gameInstanceId == null) return;
        if(_isMoving && retry == 5) return;
        UIManger.Instance.StopReceivingInput();
        Loading.StartLoadingSmall();
        _isMoving = true; 
        _prevMove = facing;
        var res = await MakeMoveTransaction(facing, energy, useCache: retry == 5 && facing != _prevMove);
        _prevMove = facing;
        Debug.Log($"Signature: {res.Result}");
        if (res.WasSuccessful)
        {
            await Web3.Rpc.ConfirmTransaction(res.Result, Commitment.Confirmed);
            Debug.Log("Made a move");
        }
        else
        {
            if (retry > 0)
            {
                Debug.Log("Retrying to move");
                await MakeMove(facing, energy, retry - 1);
                await ReloadGame(); 
            }
            else
            {
                Debug.Log("Failed to move");
                await ReloadGame(); 
            }
        }
        await ReloadGame(); 
        Loading.StopLoadingSmall();
        _isMoving = false;
        UIManger.Instance.StartReceivingInput();
    }
    
    private async UniTask MakeExplosion()
    {
        if(Web3.Account == null) return;
        if(_gameInstanceId == null) return;
        var res = await MakeExplosionTransaction();
        Debug.Log($"Signature: {res.Result}");
        if (res.WasSuccessful)
        {
            await Web3.Rpc.ConfirmTransaction(res.Result, Commitment.Confirmed);
            Debug.Log("Exploded");
            await ReloadGame();
        }
        else
        {
            Debug.Log("Failed to explode");
            await ReloadGame();
        }
    }
    
    private async UniTask SubscribeToGame(PublicKey gameId)
    {
        await ChainstrikeClient.SubscribeGameAsync(gameId, OnGameUpdate, Commitment.Processed);
    }

    private void OnGameUpdate(SubscriptionState subState, ResponseValue<AccountInfo> gameInfo, Game game)
    {
        Debug.Log("Game updated");
        SetGame(game);
    }

    private void SetGame(Game game)
    {
        Debug.Log("set game");
        if (_initPlayer) UIManger.Instance.ResetLevel();
        UIManger.Instance.SetGrid(game.Grid.Cells);
        UIManger.Instance.SetCharacters(game.Players);
        if (_initPlayer)
        {
            UIManger.Instance.ResetEnergy();
            UIManger.Instance.StartReceivingInput();
            _initPlayer = false;
        }
        if(game.GameState?.Type == GameStateType.Won && game.GameState.WonValue.Winner == Web3.Account?.PublicKey)
        {
            UIManger.Instance.ShowWinningScreen();
        }
    }
    
    #region Transactions

    private async Task<RequestResult<string>> CreateGameTransaction(string gameId, bool publicMatch = true)
    {
        var tx = new Transaction()
        {
            FeePayer = Web3.Account,
            Instructions = new List<TransactionInstruction>(),
            RecentBlockHash = await Web3.BlockHash(useCache: false)
        };
        
        var userPda = FindUserPda(Web3.Account);
        
        var matchesPda = FindMatchesPda();
        
        if (!await IsPdaInitialized(FindMatchesPda()))
        {
            var matchesInitUser = new InitializeMatchesAccounts()
            {
                Payer = Web3.Account,
                Matches = matchesPda,
                SystemProgram = SystemProgram.ProgramIdKey
            };
            var initMatchesIx = ChainstrikeProgram.InitializeMatches(accounts: matchesInitUser, _chainStrikeProgramId);
            tx.Add(initMatchesIx);
        }
        
        if (!await IsPdaInitialized(userPda))
        {
            var accountsInitUser = new InitializeUserAccounts()
            {
                Payer = Web3.Account,
                User = userPda,
                SystemProgram = SystemProgram.ProgramIdKey
            };
            var initUserIx = ChainstrikeProgram.InitializeUser(accounts: accountsInitUser, _chainStrikeProgramId);
            tx.Add(initUserIx);
        }

        var gamePda = new PublicKey(gameId);
        if (!await IsPdaInitialized(gamePda))
        {
            var accountsInitGame = new InitializeGameAccounts()
            {
                Creator = Web3.Account,
                User = userPda,
                Game = gamePda,
                Matches = publicMatch ? FindMatchesPda() : null,
                SystemProgram = SystemProgram.ProgramIdKey
            };
            var initGameIx = ChainstrikeProgram.InitializeGame(accounts: accountsInitGame, _chainStrikeProgramId);
            tx.Add(initGameIx);
        }
        
        var joinGameAccounts = new JoinGameAccounts()
        {
            Player = Web3.Account,
            User = userPda,
            Game = gamePda,
        };

        var r = new Random();
        var spawnPoint = spawnPoints[r.Next(0, spawnPoints.Length)];
        var joinGameIx = ChainstrikeProgram.JoinGame(accounts: joinGameAccounts, (byte) spawnPoint[0], (byte) spawnPoint[1], _chainStrikeProgramId);
        tx.Instructions.Add(joinGameIx);
        
        return await Web3.Wallet.SignAndSendTransaction(tx, skipPreflight: true, commitment: Commitment.Confirmed);
    }
    
    private async Task<RequestResult<string>> MakeMoveTransaction(Facing facing, int energy, bool useCache = true)
    {
        var tx = new Transaction()
        {
            FeePayer = Web3.Account,
            Instructions = new List<TransactionInstruction>(),
            RecentBlockHash = await Web3.BlockHash(commitment: Commitment.Finalized, useCache: useCache)
        };
        
        var accounts = new MakeMoveAccounts()
        {
            Player = Web3.Account,
            Game = _gameInstanceId
        };
        var movePieceIx = ChainstrikeProgram.MakeMove(accounts, facing, (byte) energy, _chainStrikeProgramId);
        
        //tx.Instructions.Add(ComputeBudgetProgram.SetComputeUnitLimit(600000));
        tx.Instructions.Add(movePieceIx);
        
        return await Web3.Wallet.SignAndSendTransaction(tx, skipPreflight: true, commitment: Commitment.Confirmed);
    }
    
    private async Task<RequestResult<string>> MakeExplosionTransaction()
    {
        var tx = new Transaction()
        {
            FeePayer = Web3.Account,
            Instructions = new List<TransactionInstruction>(),
            RecentBlockHash = await Web3.BlockHash(useCache: false)
        };
        
        var accounts = new ExplodeAccounts()
        {
            Player = Web3.Account,
            Game = _gameInstanceId
        };
        var explodeIx = ChainstrikeProgram.Explode(accounts, _chainStrikeProgramId);
        
        //tx.Instructions.Add(ComputeBudgetProgram.SetComputeUnitLimit(600000));
        tx.Instructions.Add(explodeIx);
        
        return await Web3.Wallet.SignAndSendTransaction(tx, skipPreflight: true, commitment: Commitment.Confirmed);
    }
    
    #endregion

    #region PDAs utils
    
    private async UniTask<PublicKey> FindGameToJoin()
    {
        PublicKey gameToJoin = null;
        var matchesPda = FindMatchesPda();
        var matches = await ChainstrikeClient.GetMatchesAsync(matchesPda, Commitment.Confirmed);
        if (matches.WasSuccessful && matches.ParsedResult != null)
        {
            foreach (var activeGame in matches.ParsedResult.ActiveGames.Reverse())
            {
                var game = await ChainstrikeClient.GetGameAsync(activeGame);
                if(game != null && game.WasSuccessful && game.ParsedResult != null
                   && game.ParsedResult.GameState.Type is GameStateType.Active or GameStateType.Waiting
                   && !game.ParsedResult.Players.Select(p => p.Address).Contains(Web3.Account.PublicKey))
                {
                    gameToJoin = activeGame;
                    break;
                }
            }
        }
        return gameToJoin;
    }

    private async UniTask<bool> IsPdaInitialized(PublicKey pda)
    {
        var accountInfoAsync = await Web3.Rpc.GetAccountInfoAsync(pda);
        return accountInfoAsync.WasSuccessful && accountInfoAsync.Result?.Value != null;
    }
    
    private PublicKey FindGamePda(PublicKey accountPublicKey, ulong gameId = 0)
    {
        PublicKey.TryFindProgramAddress(new[]
        {
            Encoding.UTF8.GetBytes("game"), accountPublicKey, BitConverter.GetBytes(gameId).Reverse().ToArray()
        }, _chainStrikeProgramId, out var pda, out _);
        return pda;
    }
    
    private PublicKey FindUserPda(PublicKey accountPublicKey)
    {
        PublicKey.TryFindProgramAddress(new[]
        {
            Encoding.UTF8.GetBytes("user"), accountPublicKey
        }, _chainStrikeProgramId, out var pda, out _);
        return pda;
    }
    
    private PublicKey FindMatchesPda()
    {
        PublicKey.TryFindProgramAddress(new[]
        {
            Encoding.UTF8.GetBytes("matches")
        }, _chainStrikeProgramId, out var pda, out _);
        return pda;
    }

    #endregion

}
