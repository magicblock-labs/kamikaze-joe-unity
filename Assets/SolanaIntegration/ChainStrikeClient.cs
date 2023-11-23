using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using KamikazeJoe;
using KamikazeJoe.Accounts;
using KamikazeJoe.Program;
using KamikazeJoe.Types;
using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using Solana.Unity.Programs;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Core.Sockets;
using Solana.Unity.Rpc.Messages;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Rpc.Types;
using Solana.Unity.SDK;
using Solana.Unity.Soar;
using Solana.Unity.Soar.Program;
using Solana.Unity.Wallet;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using InitializeGameAccounts = KamikazeJoe.Program.InitializeGameAccounts;
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
    
    [SerializeField]
    private TMP_InputField txtArenaSize;
    
    [SerializeField]
    private TMP_InputField txtPricePool;
    
    
    private PublicKey _gameInstanceId;
    private PublicKey _userPda;

    private readonly PublicKey _kamikazeJoeProgramId = new("JoeXD3mj5VXB2xKUz6jJ8D2AC72pXCydA6fnQJg2JiG");
    
    // Kamikaze Joe client
    private KamikazeJoeClient _kamikazeJoeClient;
    private KamikazeJoeClient KamikazeJoeClient => _kamikazeJoeClient ??= 
        new KamikazeJoeClient(Web3.Rpc, Web3.WsRpc, _kamikazeJoeProgramId);
    
    // Soar client
    private SoarClient _soarClient;
    private SoarClient SoarClient => _soarClient ??= new SoarClient(Web3.Rpc, Web3.WsRpc);

    private static readonly int[][] SpawnPoints = {
        new[] { 1, 1}, new[] { 27, 26},
        new[] { 1, 26}, new[] { 26, 1},
        new[] { 1, 15}, new[] { 26, 15},
        new[] { 7, 7}, new[] { 3, 3},
        new[] { 1, 15}, new[] { 26, 15} 
    };
    private bool _isMoving;
    private bool _initPlayer = true;
    private Facing _prevMove;
    
    private SessionWallet _sessionWallet;
    private int _prevPlayersLength = 0;
    private const string _sessionPassword = "kmzjoe-session-password";


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
        //_toast = GetComponent<Toast>();
        txtArenaSize.onEndEdit.AddListener(ClampArenaSize);
    }

    private void CallCreateGame()
    {
        if (!int.TryParse(txtArenaSize.text, out int arenaSize))
        {
            arenaSize = 30;
        }
        if (!float.TryParse(txtPricePool.text, out float pricePool))
        {
            pricePool = 0;
        }
        CreateGame(arenaSize, (ulong)(pricePool * Math.Pow(10, 9))).Forget();
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
        Debug.Log("Reloading game");
        var game = (await KamikazeJoeClient.GetGameAsync(_gameInstanceId, Commitment.Processed)).ParsedResult;
        SetGame(game);
    }

    private async UniTask JoinGame(string gameId)
    {
        if(Web3.Account == null) return;
        Loading.StartLoading();
        var game = (await KamikazeJoeClient.GetGameAsync(gameId, Commitment.Confirmed)).ParsedResult;
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
        else
        {
            if (UseSessionWallet()
                && game.GameState.Type != GameStateType.Won
                && game.Players?.FirstOrDefault(p => p.Address == Web3.Account)?.Energy != 0)
            {
                _sessionWallet = await SessionWallet.GetSessionWallet(
                    targetProgram: _kamikazeJoeProgramId,
                    password: _sessionPassword
                );
                if (!await _sessionWallet.IsSessionTokenInitialized())
                {
                    var topUp = true;
                    // Set to 3 days in unix time
                    var validity = DateTimeOffset.UtcNow.AddHours(72).ToUnixTimeSeconds();
                    var createSessionIx = _sessionWallet.CreateSessionIX(topUp, validity);
                    var tx = new Transaction()
                    {
                        FeePayer = Web3.Account,
                        Instructions = new List<TransactionInstruction>(),
                        RecentBlockHash = await Web3.BlockHash(useCache: false, commitment: Commitment.Confirmed)
                    };
                    tx.Instructions.Add(createSessionIx);
                    _sessionWallet.SignInitSessionTx(tx);
                    var res = await Web3.Wallet.SignAndSendTransaction(tx, commitment: Commitment.Confirmed);
                    if (res.WasSuccessful)
                    {
                        await Web3.Rpc.ConfirmTransaction(res.Result, Commitment.Confirmed);
                    }
                }
            }
        }
        game = (await KamikazeJoeClient.GetGameAsync(gameId, Commitment.Confirmed)).ParsedResult;
        Loading.StopLoading();
        _gameInstanceId = new PublicKey(gameId);
        UIManger.Instance.SetGameID(_gameInstanceId);
        _initPlayer = true;
        SetGame(game);
        Debug.Log($"Subscribing to game");
        SubscribeToGame(new PublicKey(gameId)).Forget();
        Debug.Log($"Game Id: {gameId}");
    }
    
    private async UniTask CreateGame(int arenaSize, ulong pricePoolLamports)
    {
        if(Web3.Account == null) return;
        Loading.StartLoading();
        var userPda = FindUserPda(Web3.Account);
        User userAccount = null;
        if (await IsPdaInitialized(userPda))
        {
            userAccount = (await KamikazeJoeClient.GetUserAsync(userPda, Commitment.Confirmed)).ParsedResult;
        }
        var gamePdaIdx = userAccount == null ? 0 : userAccount.Games;
        Debug.Log($"Searching game PDA");
        PublicKey gamePda = FindGamePda(userPda, gamePdaIdx);
        Debug.Log($"Sending transaction new Game");
        var res = await CreateGameTransaction(
            gamePda, 
            publicMatch: publicCheckBox == null || publicCheckBox.GetComponent<Image>().sprite.name.Equals("CheckboxOff"),
            arenaSize,
            pricePoolLamports);
        Debug.Log($"Signature: {res.Result}");
        if (res.WasSuccessful)
        {
            await Web3.Rpc.ConfirmTransaction(res.Result, Commitment.Confirmed);
            
            Game game = null;
            var retry = 5;
            while (game == null && retry > 0)
            {
                game = (await KamikazeJoeClient.GetGameAsync(gamePda, Commitment.Confirmed)).ParsedResult;
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
    
    private async UniTask ClaimReward(string gameId)
    {
        if(Web3.Account == null) return;
        var game = (await KamikazeJoeClient.GetGameAsync(gameId, Commitment.Confirmed)).ParsedResult;
        if(game == null) return;
        if(game.TicketPrice == 0 || game.PrizeClaimed || game.GameState?.WonValue?.Winner != Web3.Account.PublicKey) return;

        var res = await ClaimRewardAndSubmitScoreTransaction();
        Debug.Log($"Signature: {res?.Result}");
        if(res == null) return;
        if (res.WasSuccessful)
        {
            await Web3.Rpc.ConfirmTransaction(res.Result, Commitment.Confirmed);
        }
        Debug.Log("Claimed Reward");
    }
    
    private async UniTask MakeMove(Facing facing, int energy, int retry = 5)
    {
        if(Web3.Account == null) return;
        if(_gameInstanceId == null) return;
        if(_isMoving && retry == 5) return;
        UIManger.Instance.StopReceivingInput();
        Loading.StartLoadingSmall();
        _isMoving = true; 
        var res = await MakeMoveTransaction(facing, energy, useCache: retry == 5 && facing != _prevMove);
        _prevMove = facing;
        Debug.Log($"Signature: {res.Result}");
        if (res.WasSuccessful)
        {
            await Web3.Rpc.ConfirmTransaction(res.Result, Commitment.Confirmed);
            _prevMove = facing;
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
        await UIManger.Instance.WaitCharacterIdle();
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
        await KamikazeJoeClient.SubscribeGameAsync(gameId, OnGameUpdate, Commitment.Processed);
    }

    private void OnGameUpdate(SubscriptionState subState, ResponseValue<AccountInfo> gameInfo, Game game)
    {
        Debug.Log("Game updated");
        SetGame(game);
    }

    private async void SetGame(Game game)
    {
        if (_initPlayer) UIManger.Instance.ResetLevel();
        UIManger.Instance.SetGrid(BuildCells(game.Width, game.Height, game.Seed));
        UIManger.Instance.SetCharacters(game.Players);
        if (_initPlayer)
        {
            UIManger.Instance.ResetEnergy();
            UIManger.Instance.StartReceivingInput();
            _initPlayer = false;
        }

        if (game.Players.Length != _prevPlayersLength)
        {
            var pricePool = game.TicketPrice / Math.Pow(10, 9) * game.Players.Length * 0.9;
            UIManger.Instance.SetPrizePool((float)Math.Round(pricePool, 2));
            _prevPlayersLength = game.Players.Length;
        }
        if(game.GameState?.Type == GameStateType.Won && game.GameState.WonValue.Winner == Web3.Account?.PublicKey)
        {
            UIManger.Instance.ShowWinningScreen();
            ClaimReward(_gameInstanceId).Forget();
        }

        // If won or lost, close session
        if (game.GameState?.Type == GameStateType.Won || game.Players?.FirstOrDefault(p => p.Address == Web3.Account)?.Energy == 0)
        {
            if(Web3.Account != null && game.GameState?.WonValue?.Winner == Web3.Account.PublicKey && !game.PrizeClaimed) return;
            if(_sessionWallet != null && await _sessionWallet.IsSessionTokenInitialized())
                _sessionWallet.CloseSession().AsUniTask().Forget();
        }
    }
    
    #region Transactions

        private async Task<RequestResult<string>> CreateGameTransaction(
            string gameId, 
            bool publicMatch = true, 
            int arenaSize = 30, 
            ulong pricePoolLamports = 0)
        {
            var tx = new Transaction()
            {
                FeePayer = Web3.Account,
                Instructions = new List<TransactionInstruction>(),
                RecentBlockHash = await Web3.BlockHash(useCache: false, commitment: Commitment.Confirmed)
            };
            
            var userPda = FindUserPda(Web3.Account);
            var matchesPda = FindMatchesPda();
            var vaultPda = FindVaultPda();
            
            if (!await IsPdaInitialized(vaultPda))
            {
                var initializeAccounts = new InitializeAccounts()
                {
                    Payer = Web3.Account,
                    Matches = matchesPda,
                    Vault = vaultPda,
                    SystemProgram = SystemProgram.ProgramIdKey
                };
                var initIx = KamikazeJoeProgram.Initialize(accounts: initializeAccounts, _kamikazeJoeProgramId);
                tx.Add(initIx);
            }
            
            if (!await IsPdaInitialized(userPda))
            {
                var accountsInitUser = new InitializeUserAccounts()
                {
                    Payer = Web3.Account,
                    User = userPda,
                    SystemProgram = SystemProgram.ProgramIdKey
                };
                var initUserIx = KamikazeJoeProgram.InitializeUser(accounts: accountsInitUser, _kamikazeJoeProgramId);
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
                var initGameIx = KamikazeJoeProgram.InitializeGame(
                    accounts: accountsInitGame, 
                    (byte?)arenaSize,
                    (byte?)arenaSize,
                    null,
                    (ulong?)pricePoolLamports,
                    _kamikazeJoeProgramId
                );
                tx.Add(initGameIx);
            }

            var joinGameAccounts = new JoinGameAccounts()
            {
                Player = Web3.Account,
                User = userPda,
                Game = gamePda,
                Vault = FindVaultPda(),
                SystemProgram = SystemProgram.ProgramIdKey
            };

            var spawnPoint = FindValidSpawnPoint(30, 30, 0);
            var joinGameIx = KamikazeJoeProgram.JoinGame(accounts: joinGameAccounts, (byte) spawnPoint[0], (byte) spawnPoint[1], _kamikazeJoeProgramId);
            tx.Instructions.Add(joinGameIx);
            
            #region Soar initialization
            
            // Soar initialization
            var playerAccount = SoarPda.PlayerPda(Web3.Account);
            var leaderboard = (await KamikazeJoeClient.GetLeaderboardAsync(FindSoarPda())).ParsedResult;
            var playerScores = SoarPda.PlayerScoresPda(playerAccount, leaderboard.LeaderboardField);
            
            if (!await IsPdaInitialized(SoarPda.PlayerPda(Web3.Account)))
            {
                var accountsInitPlayer = new InitializePlayerAccounts()
                {
                    Payer = Web3.Account,
                    User = Web3.Account,
                    PlayerAccount = playerAccount,
                    SystemProgram = SystemProgram.ProgramIdKey
                };
                var initPlayerIx = SoarProgram.InitializePlayer(
                    accounts: accountsInitPlayer,
                    username: PlayerPrefs.GetString("web3AuthUsername", ""),
                    nftMeta: PublicKey.DefaultPublicKey,
                    SoarProgram.ProgramIdKey
                );
                tx.Add(initPlayerIx);
            
            }
            
            if (!await IsPdaInitialized(playerScores))
            {
                var registerPlayerAccounts = new RegisterPlayerAccounts()
                {
                    Payer = Web3.Account,
                    User = Web3.Account,
                    PlayerAccount = playerAccount,
                    Game = leaderboard.Game,
                    Leaderboard = leaderboard.LeaderboardField,
                    NewList = playerScores,
                    SystemProgram = SystemProgram.ProgramIdKey
                };
                var registerPlayerIx = SoarProgram.RegisterPlayer(
                    registerPlayerAccounts,
                    SoarProgram.ProgramIdKey
                );
                tx.Add(registerPlayerIx);
            }
            
            #endregion
            
            if (UseSessionWallet())
            {
                _sessionWallet = await SessionWallet.GetSessionWallet(
                    targetProgram: _kamikazeJoeProgramId, 
                    password: _sessionPassword
                );

                var isSessionInitialized = await _sessionWallet.IsSessionTokenInitialized();

                if(isSessionInitialized) tx.Add(_sessionWallet.RevokeSessionIX());

                var topUp = true;
                // Set to 3 days in unix time
                var validity = DateTimeOffset.UtcNow.AddHours(72).ToUnixTimeSeconds();
                var createSessionIx = _sessionWallet.CreateSessionIX(topUp, validity);
                tx.Instructions.Add(createSessionIx);
                _sessionWallet.SignInitSessionTx(tx);
            }

            return await Web3.Wallet.SignAndSendTransaction(tx, skipPreflight: false, commitment: Commitment.Confirmed);
        }

        private static bool UseSessionWallet()
        {
            return Web3.Wallet.GetType() != typeof(InGameWallet) && Web3.Wallet.GetType() != typeof(Web3AuthWallet);
        }

        private int[] FindValidSpawnPoint(int width, int height, uint seed)
        {
            var r = new Random();
            var point = SpawnPoints[r.Next(0, SpawnPoints.Length)];
            while (!IsValidCell(point[0], point[1], width, height, seed))
            {
                point = SpawnPoints[r.Next(0, SpawnPoints.Length)];
            }
            return point;
        }

        private async Task<RequestResult<string>> MakeMoveTransaction(Facing facing, int energy, bool useCache = true)
        {
            var tx = new Transaction()
            {
                FeePayer = _sessionWallet != null ? _sessionWallet.Account : Web3.Account,
                Instructions = new List<TransactionInstruction>(),
                RecentBlockHash = await Web3.BlockHash(commitment: Commitment.Confirmed, useCache: useCache)
            };
        
            var accounts = new MakeMoveAccounts()
            {
                Payer = Web3.Account,
                User = _userPda != null ? _userPda : FindUserPda(Web3.Account),
                Game = _gameInstanceId,
            };
            if (_sessionWallet != null)
            {
                accounts.Payer = _sessionWallet.Account;
                accounts.SessionToken = _sessionWallet.SessionTokenPDA;
            }
            
            var movePieceIx = KamikazeJoeProgram.MakeMove(accounts, facing, (byte) energy, _kamikazeJoeProgramId);
        
            //tx.Instructions.Add(ComputeBudgetProgram.SetComputeUnitLimit(600000));
            tx.Instructions.Add(movePieceIx);

            return await SignAndSendTransaction(tx);
        }
    

        private async Task<RequestResult<string>> MakeExplosionTransaction()
        {
            var tx = new Transaction()
            {
                FeePayer = Web3.Account,
                Instructions = new List<TransactionInstruction>(),
                RecentBlockHash = await Web3.BlockHash(commitment: Commitment.Confirmed, useCache: false)
            };
        
            var accounts = new ExplodeAccounts()
            {
                Payer = Web3.Account,
                User = _userPda != null ? _userPda : FindUserPda(Web3.Account),
                Game = _gameInstanceId
            };
            if (_sessionWallet != null)
            {
                accounts.Payer = _sessionWallet.Account;
                accounts.SessionToken = _sessionWallet.SessionTokenPDA;
            }
            var explodeIx = KamikazeJoeProgram.Explode(accounts, _kamikazeJoeProgramId);
        
            tx.Instructions.Add(ComputeBudgetProgram.SetComputeUnitLimit(600000));
            tx.Instructions.Add(explodeIx);
        
            return await SignAndSendTransaction(tx);
        }

        private async Task<RequestResult<string>> ClaimRewardTransaction()
        {
            var game = (await KamikazeJoeClient.GetGameAsync(_gameInstanceId, Commitment.Confirmed)).ParsedResult;
            if (game == null || game.GameState?.WonValue?.Winner == null)
            {
                Debug.LogError($"Can't claim price for game: {_gameInstanceId}");
                return null;
            }
            var tx = new Transaction()
            {
                FeePayer = Web3.Account,
                Instructions = new List<TransactionInstruction>(),
                RecentBlockHash = await Web3.BlockHash(commitment: Commitment.Confirmed, useCache: false)
            };

            var claimPrizeAccounts = new ClaimPrizeAccounts()
            {
                Payer = Web3.Account,
                User = FindUserPda(game.GameState.WonValue.Winner),
                Receiver = game.GameState.WonValue.Winner,
                Game = _gameInstanceId,
                Vault = FindVaultPda(),
                SystemProgram = SystemProgram.ProgramIdKey
            };
            if (_sessionWallet != null)
            {
                claimPrizeAccounts.Payer = _sessionWallet.Account;
            }
        
            var claimPrizeIx = KamikazeJoeProgram.ClaimPrize(accounts: claimPrizeAccounts, _kamikazeJoeProgramId);
            tx.Instructions.Add(claimPrizeIx);
        
            return await SignAndSendTransaction(tx);
        }
        
        private async Task<RequestResult<string>> ClaimRewardAndSubmitScoreTransaction()
        {
            var game = (await KamikazeJoeClient.GetGameAsync(_gameInstanceId, Commitment.Confirmed)).ParsedResult;
            var soar = (await KamikazeJoeClient.GetLeaderboardAsync(FindSoarPda())).ParsedResult;
            if (game == null || game.GameState?.WonValue?.Winner == null)
            {
                Debug.LogError($"Can't claim price for game: {_gameInstanceId}");
                return null;
            }

            if (soar == null)
            {
                Debug.LogError("Leaderboard not initialized");
                return null;
            }
            var tx = new Transaction()
            {
                FeePayer = Web3.Account,
                Instructions = new List<TransactionInstruction>(),
                RecentBlockHash = await Web3.BlockHash(commitment: Commitment.Confirmed, useCache: false)
            };

            var playerAccount = SoarPda.PlayerPda(game.GameState.WonValue.Winner);

            var claimPrizeAccounts = new ClaimPrizeSoarAccounts()
            {
                Payer = Web3.Account,
                User = FindUserPda(game.GameState.WonValue.Winner),
                Receiver = game.GameState.WonValue.Winner,
                Game = _gameInstanceId,
                Vault = FindVaultPda(),
                LeaderboardInfo = FindSoarPda(),
                SoarGame = soar.Game,
                SoarLeaderboard = soar.LeaderboardField,
                SoarPlayerAccount = playerAccount,
                SoarPlayerScores = SoarPda.PlayerScoresPda(playerAccount, soar.LeaderboardField),
                SoarTopEntries = soar.TopEntries,
                SoarProgram = SoarProgram.ProgramIdKey,
                SystemProgram = SystemProgram.ProgramIdKey
            };
            if (_sessionWallet != null)
            {
                claimPrizeAccounts.Payer = _sessionWallet.Account;
            }
        
            var claimPrizeIx = KamikazeJoeProgram.ClaimPrizeSoar(accounts: claimPrizeAccounts, _kamikazeJoeProgramId);
            tx.Instructions.Add(claimPrizeIx);
        
            return await SignAndSendTransaction(tx);
        }
        
        private async Task<RequestResult<string>> SignAndSendTransaction(Transaction tx)
        {
            if (_sessionWallet != null)
            {
                tx.FeePayer = _sessionWallet.Account;
                return await _sessionWallet.SignAndSendTransaction(tx, skipPreflight: true, commitment: Commitment.Confirmed);
            }
            else
            {
                return await Web3.Wallet.SignAndSendTransaction(tx, skipPreflight: true, commitment: Commitment.Confirmed);
            }
        }
    
        #endregion

        #region PDAs utils
    
        private async UniTask<PublicKey> FindGameToJoin()
        {
            PublicKey gameToJoin = null;
            var matchesPda = FindMatchesPda();
            var matches = await KamikazeJoeClient.GetMatchesAsync(matchesPda, Commitment.Confirmed);
            if (matches.WasSuccessful && matches.ParsedResult != null)
            {
                foreach (var activeGame in matches.ParsedResult.ActiveGames.Reverse())
                {
                    var game = await KamikazeJoeClient.GetGameAsync(activeGame, Commitment.Confirmed);
                    if(game != null && game.WasSuccessful && game.ParsedResult != null
                       && game.ParsedResult.GameState.Type is GameStateType.Active or GameStateType.Waiting
                       && !game.ParsedResult.Players.Select(p => p.Address).Contains(Web3.Account.PublicKey)
                       && game.ParsedResult.Players.Length < 10)
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
    
        private PublicKey FindGamePda(PublicKey accountPublicKey, uint gameId = 0)
        {
            PublicKey.TryFindProgramAddress(new[]
            {
                Encoding.UTF8.GetBytes("game"), accountPublicKey, BitConverter.GetBytes(gameId).Reverse().ToArray()
            }, _kamikazeJoeProgramId, out var pda, out _);
            return pda;
        }
    
        private PublicKey FindUserPda(PublicKey accountPublicKey)
        {
            PublicKey.TryFindProgramAddress(new[]
            {
                Encoding.UTF8.GetBytes("user-pda"), accountPublicKey
            }, _kamikazeJoeProgramId, out var pda, out _);
            return pda;
        }
    
        private PublicKey FindMatchesPda()
        {
            PublicKey.TryFindProgramAddress(new[]
            {
                Encoding.UTF8.GetBytes("matches")
            }, _kamikazeJoeProgramId, out var pda, out _);
            return pda;
        }
        
        private PublicKey FindSoarPda()
        {
            PublicKey.TryFindProgramAddress(new[]
            {
                Encoding.UTF8.GetBytes("soar")
            }, _kamikazeJoeProgramId, out var pda, out _);
            return pda;
        }
    
        private PublicKey FindVaultPda()
        {
            PublicKey.TryFindProgramAddress(new[]
            {
                Encoding.UTF8.GetBytes("vault")
            }, _kamikazeJoeProgramId, out var pda, out _);
            return pda;
        }

        #endregion
    
        #region Build Grid

        private Cell[][] BuildCells(uint width, uint height, uint seed)
        {
            Cell[][] cells = new Cell[height][];
            for (int x = 0; x < width; x++)
            {
                cells[x] = new Cell[height];
                for (int y = 0; y < height; y++)
                {
                    if (x < width && y < height && IsRecharger(x, y, seed))
                    {
                        cells[x][y] = Cell.Recharge;
                    }
                    else if (x < width && y < height && IsBlock(x, y, seed))
                    {
                        cells[x][y] = Cell.Block;
                    }
                    else
                    {
                        cells[x][y] = Cell.Empty;
                    }
                }
            }
            return cells;
        }

        private bool IsValidCell(int x, int y, int width, int height, uint seed)
        {
            return x < width && y < height && (IsRecharger(x, y, seed) || !IsBlock(x, y, seed));
        }

        private bool IsRecharger(int x, int y, uint seed)
        {  
            uint shift = seed % 14;
            long xPlusShift = x + shift;
            long yMinusShift = y - shift;

            long xMod13 = xPlusShift % 13;
            long yMod14 = yMinusShift % 14;
            long xMod28 = xPlusShift % 28;
            long yMod28 = yMinusShift % 28;

            return xMod13 == yMod14
                   && (xMod28 is 27 || xPlusShift == 1)
                   && xPlusShift != yMinusShift 
                   && xMod28 - yMod28 < 15;
        }

        private bool IsBlock(int x, int y, uint seed)
        {
            uint len = 4 + seed % 6;
            int xMod28 = x % 28;
            int yMod28 = y % 28;

            if ((yMod28 == 5 && xMod28 > 3 && xMod28 < 3 + len) ||
                (yMod28 == 23 && xMod28 > 7 && xMod28 < 7 + Math.Max(5, len)) ||
                (yMod28 == 12 && xMod28 > 12 && xMod28 < 12 + len) ||
                (xMod28 == 19 && yMod28 > 12 && yMod28 < 12 + Math.Max(5, len)))
            {
                return true;
            }

            int xSquaredPlusY = x * x + x * y;
            int ySquared = y * y;
            uint divisor = 47 % 60 - seed % 59;
            long remainder = (xSquaredPlusY + ySquared + seed) % divisor;

            return remainder == 7;
        }

        #endregion

        #region Game Utils

        private void ClampArenaSize(string value)
        {
            if (!int.TryParse(value, out var arenaSize)) return;
            txtArenaSize.text = arenaSize switch
            {
                < 10 => "25",
                > 150 => "150",
                _ => txtArenaSize.text
            };
        }

        #endregion

}
