using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Solana.Unity;
using Solana.Unity.Programs.Abstract;
using Solana.Unity.Programs.Utilities;
using Solana.Unity.Rpc;
using Solana.Unity.Rpc.Builders;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Core.Sockets;
using Solana.Unity.Rpc.Types;
using Solana.Unity.Wallet;
using KamikazeJoe;
using KamikazeJoe.Program;
using KamikazeJoe.Errors;
using KamikazeJoe.Accounts;
using KamikazeJoe.Types;

namespace KamikazeJoe
{
    namespace Accounts
    {
        public partial class Game
        {
            public static ulong ACCOUNT_DISCRIMINATOR => 1331205435963103771UL;
            public static ReadOnlySpan<byte> ACCOUNT_DISCRIMINATOR_BYTES => new byte[]{27, 90, 166, 125, 74, 100, 121, 18};
            public static string ACCOUNT_DISCRIMINATOR_B58 => "5aNQXizG8jB";
            public uint Id { get; set; }

            public byte Width { get; set; }

            public byte Height { get; set; }

            public byte Seed { get; set; }

            public ulong TicketPrice { get; set; }

            public bool PrizeClaimed { get; set; }

            public PublicKey Owner { get; set; }

            public GameState GameState { get; set; }

            public Player[] Players { get; set; }

            public static Game Deserialize(ReadOnlySpan<byte> _data)
            {
                int offset = 0;
                ulong accountHashValue = _data.GetU64(offset);
                offset += 8;
                if (accountHashValue != ACCOUNT_DISCRIMINATOR)
                {
                    return null;
                }

                Game result = new Game();
                result.Id = _data.GetU32(offset);
                offset += 4;
                result.Width = _data.GetU8(offset);
                offset += 1;
                result.Height = _data.GetU8(offset);
                offset += 1;
                result.Seed = _data.GetU8(offset);
                offset += 1;
                result.TicketPrice = _data.GetU64(offset);
                offset += 8;
                result.PrizeClaimed = _data.GetBool(offset);
                offset += 1;
                result.Owner = _data.GetPubKey(offset);
                offset += 32;
                offset += GameState.Deserialize(_data, offset, out var resultGameState);
                result.GameState = resultGameState;
                int resultPlayersLength = (int)_data.GetU32(offset);
                offset += 4;
                result.Players = new Player[resultPlayersLength];
                for (uint resultPlayersIdx = 0; resultPlayersIdx < resultPlayersLength; resultPlayersIdx++)
                {
                    offset += Player.Deserialize(_data, offset, out var resultPlayersresultPlayersIdx);
                    result.Players[resultPlayersIdx] = resultPlayersresultPlayersIdx;
                }

                return result;
            }
        }

        public partial class Leaderboard
        {
            public static ulong ACCOUNT_DISCRIMINATOR => 2596640482820799223UL;
            public static ReadOnlySpan<byte> ACCOUNT_DISCRIMINATOR_BYTES => new byte[]{247, 186, 238, 243, 194, 30, 9, 36};
            public static string ACCOUNT_DISCRIMINATOR_B58 => "iSJ1okut6kT";
            public PublicKey Game { get; set; }

            public PublicKey LeaderboardField { get; set; }

            public PublicKey TopEntries { get; set; }

            public static Leaderboard Deserialize(ReadOnlySpan<byte> _data)
            {
                int offset = 0;
                ulong accountHashValue = _data.GetU64(offset);
                offset += 8;
                if (accountHashValue != ACCOUNT_DISCRIMINATOR)
                {
                    return null;
                }

                Leaderboard result = new Leaderboard();
                result.Game = _data.GetPubKey(offset);
                offset += 32;
                result.LeaderboardField = _data.GetPubKey(offset);
                offset += 32;
                result.TopEntries = _data.GetPubKey(offset);
                offset += 32;
                return result;
            }
        }

        public partial class Matches
        {
            public static ulong ACCOUNT_DISCRIMINATOR => 14715191942542898288UL;
            public static ReadOnlySpan<byte> ACCOUNT_DISCRIMINATOR_BYTES => new byte[]{112, 180, 245, 120, 27, 221, 54, 204};
            public static string ACCOUNT_DISCRIMINATOR_B58 => "KrQ5Hnb29FR";
            public PublicKey[] ActiveGames { get; set; }

            public static Matches Deserialize(ReadOnlySpan<byte> _data)
            {
                int offset = 0;
                ulong accountHashValue = _data.GetU64(offset);
                offset += 8;
                if (accountHashValue != ACCOUNT_DISCRIMINATOR)
                {
                    return null;
                }

                Matches result = new Matches();
                int resultActiveGamesLength = (int)_data.GetU32(offset);
                offset += 4;
                result.ActiveGames = new PublicKey[resultActiveGamesLength];
                for (uint resultActiveGamesIdx = 0; resultActiveGamesIdx < resultActiveGamesLength; resultActiveGamesIdx++)
                {
                    result.ActiveGames[resultActiveGamesIdx] = _data.GetPubKey(offset);
                    offset += 32;
                }

                return result;
            }
        }

        public partial class User
        {
            public static ulong ACCOUNT_DISCRIMINATOR => 17022084798167872927UL;
            public static ReadOnlySpan<byte> ACCOUNT_DISCRIMINATOR_BYTES => new byte[]{159, 117, 95, 227, 239, 151, 58, 236};
            public static string ACCOUNT_DISCRIMINATOR_B58 => "TfwwBiNJtao";
            public PublicKey Authority { get; set; }

            public uint Games { get; set; }

            public uint Won { get; set; }

            public static User Deserialize(ReadOnlySpan<byte> _data)
            {
                int offset = 0;
                ulong accountHashValue = _data.GetU64(offset);
                offset += 8;
                if (accountHashValue != ACCOUNT_DISCRIMINATOR)
                {
                    return null;
                }

                User result = new User();
                result.Authority = _data.GetPubKey(offset);
                offset += 32;
                result.Games = _data.GetU32(offset);
                offset += 4;
                result.Won = _data.GetU32(offset);
                offset += 4;
                return result;
            }
        }

        public partial class Vault
        {
            public static ulong ACCOUNT_DISCRIMINATOR => 8607953397882554579UL;
            public static ReadOnlySpan<byte> ACCOUNT_DISCRIMINATOR_BYTES => new byte[]{211, 8, 232, 43, 2, 152, 117, 119};
            public static string ACCOUNT_DISCRIMINATOR_B58 => "cJJWPqNMczr";
            public static Vault Deserialize(ReadOnlySpan<byte> _data)
            {
                int offset = 0;
                ulong accountHashValue = _data.GetU64(offset);
                offset += 8;
                if (accountHashValue != ACCOUNT_DISCRIMINATOR)
                {
                    return null;
                }

                Vault result = new Vault();
                return result;
            }
        }
    }

    namespace Errors
    {
        public enum KamikazeJoeErrorKind : uint
        {
            InvalidSize = 6000U,
            GameEnded = 6001U,
            PlayerNotFound = 6002U,
            NotValidEnergy = 6003U,
            MovingIntoNotEmptyCell = 6004U,
            InvalidMovement = 6005U,
            InvalidJoin = 6006U,
            InvalidClaim = 6007U,
            Overflow = 6008U,
            InvalidUser = 6009U,
            InvalidAuthority = 6010U
        }
    }

    namespace Types
    {
        public partial class Player
        {
            public byte X { get; set; }

            public byte Y { get; set; }

            public byte Energy { get; set; }

            public PublicKey Address { get; set; }

            public Facing Facing { get; set; }

            public int Serialize(byte[] _data, int initialOffset)
            {
                int offset = initialOffset;
                _data.WriteU8(X, offset);
                offset += 1;
                _data.WriteU8(Y, offset);
                offset += 1;
                _data.WriteU8(Energy, offset);
                offset += 1;
                _data.WritePubKey(Address, offset);
                offset += 32;
                _data.WriteU8((byte)Facing, offset);
                offset += 1;
                return offset - initialOffset;
            }

            public static int Deserialize(ReadOnlySpan<byte> _data, int initialOffset, out Player result)
            {
                int offset = initialOffset;
                result = new Player();
                result.X = _data.GetU8(offset);
                offset += 1;
                result.Y = _data.GetU8(offset);
                offset += 1;
                result.Energy = _data.GetU8(offset);
                offset += 1;
                result.Address = _data.GetPubKey(offset);
                offset += 32;
                result.Facing = (Facing)_data.GetU8(offset);
                offset += 1;
                return offset - initialOffset;
            }
        }

        public enum GameStateType : byte
        {
            Waiting,
            Active,
            Won
        }

        public partial class WonType
        {
            public PublicKey Winner { get; set; }

            public static int Deserialize(ReadOnlySpan<byte> _data, int initialOffset, out WonType result)
            {
                int offset = initialOffset;
                result = new WonType();
                result.Winner = _data.GetPubKey(offset);
                offset += 32;
                return offset - initialOffset;
            }

            public int Serialize(byte[] _data, int initialOffset)
            {
                int offset = initialOffset;
                _data.WritePubKey(Winner, offset);
                offset += 32;
                return offset - initialOffset;
            }
        }

        public partial class GameState
        {
            public WonType WonValue { get; set; }

            public int Serialize(byte[] _data, int initialOffset)
            {
                int offset = initialOffset;
                _data.WriteU8((byte)Type, offset);
                offset += 1;
                switch (Type)
                {
                    case GameStateType.Won:
                        offset += WonValue.Serialize(_data, offset);
                        break;
                }

                return offset - initialOffset;
            }

            public GameStateType Type { get; set; }

            public static int Deserialize(ReadOnlySpan<byte> _data, int initialOffset, out GameState result)
            {
                int offset = initialOffset;
                result = new GameState();
                result.Type = (GameStateType)_data.GetU8(offset);
                offset += 1;
                switch (result.Type)
                {
                    case GameStateType.Won:
                    {
                        WonType tmpWonValue = new WonType();
                        offset += WonType.Deserialize(_data, offset, out tmpWonValue);
                        result.WonValue = tmpWonValue;
                        break;
                    }
                }

                return offset - initialOffset;
            }
        }

        public enum Cell : byte
        {
            Empty,
            Block,
            Recharge
        }

        public enum Facing : byte
        {
            Up,
            Down,
            Left,
            Right
        }
    }

    public partial class KamikazeJoeClient : TransactionalBaseClient<KamikazeJoeErrorKind>
    {
        public KamikazeJoeClient(IRpcClient rpcClient, IStreamingRpcClient streamingRpcClient, PublicKey programId) : base(rpcClient, streamingRpcClient, programId)
        {
        }

        public async Task<Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<Game>>> GetGamesAsync(string programAddress, Commitment commitment = Commitment.Finalized)
        {
            var list = new List<Solana.Unity.Rpc.Models.MemCmp>{new Solana.Unity.Rpc.Models.MemCmp{Bytes = Game.ACCOUNT_DISCRIMINATOR_B58, Offset = 0}};
            var res = await RpcClient.GetProgramAccountsAsync(programAddress, commitment, memCmpList: list);
            if (!res.WasSuccessful || !(res.Result?.Count > 0))
                return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<Game>>(res);
            List<Game> resultingAccounts = new List<Game>(res.Result.Count);
            resultingAccounts.AddRange(res.Result.Select(result => Game.Deserialize(Convert.FromBase64String(result.Account.Data[0]))));
            return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<Game>>(res, resultingAccounts);
        }

        public async Task<Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<Leaderboard>>> GetLeaderboardsAsync(string programAddress, Commitment commitment = Commitment.Finalized)
        {
            var list = new List<Solana.Unity.Rpc.Models.MemCmp>{new Solana.Unity.Rpc.Models.MemCmp{Bytes = Leaderboard.ACCOUNT_DISCRIMINATOR_B58, Offset = 0}};
            var res = await RpcClient.GetProgramAccountsAsync(programAddress, commitment, memCmpList: list);
            if (!res.WasSuccessful || !(res.Result?.Count > 0))
                return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<Leaderboard>>(res);
            List<Leaderboard> resultingAccounts = new List<Leaderboard>(res.Result.Count);
            resultingAccounts.AddRange(res.Result.Select(result => Leaderboard.Deserialize(Convert.FromBase64String(result.Account.Data[0]))));
            return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<Leaderboard>>(res, resultingAccounts);
        }

        public async Task<Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<Matches>>> GetMatchessAsync(string programAddress, Commitment commitment = Commitment.Finalized)
        {
            var list = new List<Solana.Unity.Rpc.Models.MemCmp>{new Solana.Unity.Rpc.Models.MemCmp{Bytes = Matches.ACCOUNT_DISCRIMINATOR_B58, Offset = 0}};
            var res = await RpcClient.GetProgramAccountsAsync(programAddress, commitment, memCmpList: list);
            if (!res.WasSuccessful || !(res.Result?.Count > 0))
                return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<Matches>>(res);
            List<Matches> resultingAccounts = new List<Matches>(res.Result.Count);
            resultingAccounts.AddRange(res.Result.Select(result => Matches.Deserialize(Convert.FromBase64String(result.Account.Data[0]))));
            return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<Matches>>(res, resultingAccounts);
        }

        public async Task<Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<User>>> GetUsersAsync(string programAddress, Commitment commitment = Commitment.Finalized)
        {
            var list = new List<Solana.Unity.Rpc.Models.MemCmp>{new Solana.Unity.Rpc.Models.MemCmp{Bytes = User.ACCOUNT_DISCRIMINATOR_B58, Offset = 0}};
            var res = await RpcClient.GetProgramAccountsAsync(programAddress, commitment, memCmpList: list);
            if (!res.WasSuccessful || !(res.Result?.Count > 0))
                return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<User>>(res);
            List<User> resultingAccounts = new List<User>(res.Result.Count);
            resultingAccounts.AddRange(res.Result.Select(result => User.Deserialize(Convert.FromBase64String(result.Account.Data[0]))));
            return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<User>>(res, resultingAccounts);
        }

        public async Task<Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<Vault>>> GetVaultsAsync(string programAddress, Commitment commitment = Commitment.Finalized)
        {
            var list = new List<Solana.Unity.Rpc.Models.MemCmp>{new Solana.Unity.Rpc.Models.MemCmp{Bytes = Vault.ACCOUNT_DISCRIMINATOR_B58, Offset = 0}};
            var res = await RpcClient.GetProgramAccountsAsync(programAddress, commitment, memCmpList: list);
            if (!res.WasSuccessful || !(res.Result?.Count > 0))
                return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<Vault>>(res);
            List<Vault> resultingAccounts = new List<Vault>(res.Result.Count);
            resultingAccounts.AddRange(res.Result.Select(result => Vault.Deserialize(Convert.FromBase64String(result.Account.Data[0]))));
            return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<Vault>>(res, resultingAccounts);
        }

        public async Task<Solana.Unity.Programs.Models.AccountResultWrapper<Game>> GetGameAsync(string accountAddress, Commitment commitment = Commitment.Finalized)
        {
            var res = await RpcClient.GetAccountInfoAsync(accountAddress, commitment);
            if (!res.WasSuccessful)
                return new Solana.Unity.Programs.Models.AccountResultWrapper<Game>(res);
            var resultingAccount = Game.Deserialize(Convert.FromBase64String(res.Result.Value.Data[0]));
            return new Solana.Unity.Programs.Models.AccountResultWrapper<Game>(res, resultingAccount);
        }

        public async Task<Solana.Unity.Programs.Models.AccountResultWrapper<Leaderboard>> GetLeaderboardAsync(string accountAddress, Commitment commitment = Commitment.Finalized)
        {
            var res = await RpcClient.GetAccountInfoAsync(accountAddress, commitment);
            if (!res.WasSuccessful)
                return new Solana.Unity.Programs.Models.AccountResultWrapper<Leaderboard>(res);
            var resultingAccount = Leaderboard.Deserialize(Convert.FromBase64String(res.Result.Value.Data[0]));
            return new Solana.Unity.Programs.Models.AccountResultWrapper<Leaderboard>(res, resultingAccount);
        }

        public async Task<Solana.Unity.Programs.Models.AccountResultWrapper<Matches>> GetMatchesAsync(string accountAddress, Commitment commitment = Commitment.Finalized)
        {
            var res = await RpcClient.GetAccountInfoAsync(accountAddress, commitment);
            if (!res.WasSuccessful)
                return new Solana.Unity.Programs.Models.AccountResultWrapper<Matches>(res);
            var resultingAccount = Matches.Deserialize(Convert.FromBase64String(res.Result.Value.Data[0]));
            return new Solana.Unity.Programs.Models.AccountResultWrapper<Matches>(res, resultingAccount);
        }

        public async Task<Solana.Unity.Programs.Models.AccountResultWrapper<User>> GetUserAsync(string accountAddress, Commitment commitment = Commitment.Finalized)
        {
            var res = await RpcClient.GetAccountInfoAsync(accountAddress, commitment);
            if (!res.WasSuccessful)
                return new Solana.Unity.Programs.Models.AccountResultWrapper<User>(res);
            var resultingAccount = User.Deserialize(Convert.FromBase64String(res.Result.Value.Data[0]));
            return new Solana.Unity.Programs.Models.AccountResultWrapper<User>(res, resultingAccount);
        }

        public async Task<Solana.Unity.Programs.Models.AccountResultWrapper<Vault>> GetVaultAsync(string accountAddress, Commitment commitment = Commitment.Finalized)
        {
            var res = await RpcClient.GetAccountInfoAsync(accountAddress, commitment);
            if (!res.WasSuccessful)
                return new Solana.Unity.Programs.Models.AccountResultWrapper<Vault>(res);
            var resultingAccount = Vault.Deserialize(Convert.FromBase64String(res.Result.Value.Data[0]));
            return new Solana.Unity.Programs.Models.AccountResultWrapper<Vault>(res, resultingAccount);
        }

        public async Task<SubscriptionState> SubscribeGameAsync(string accountAddress, Action<SubscriptionState, Solana.Unity.Rpc.Messages.ResponseValue<Solana.Unity.Rpc.Models.AccountInfo>, Game> callback, Commitment commitment = Commitment.Finalized)
        {
            SubscriptionState res = await StreamingRpcClient.SubscribeAccountInfoAsync(accountAddress, (s, e) =>
            {
                Game parsingResult = null;
                if (e.Value?.Data?.Count > 0)
                    parsingResult = Game.Deserialize(Convert.FromBase64String(e.Value.Data[0]));
                callback(s, e, parsingResult);
            }, commitment);
            return res;
        }

        public async Task<SubscriptionState> SubscribeLeaderboardAsync(string accountAddress, Action<SubscriptionState, Solana.Unity.Rpc.Messages.ResponseValue<Solana.Unity.Rpc.Models.AccountInfo>, Leaderboard> callback, Commitment commitment = Commitment.Finalized)
        {
            SubscriptionState res = await StreamingRpcClient.SubscribeAccountInfoAsync(accountAddress, (s, e) =>
            {
                Leaderboard parsingResult = null;
                if (e.Value?.Data?.Count > 0)
                    parsingResult = Leaderboard.Deserialize(Convert.FromBase64String(e.Value.Data[0]));
                callback(s, e, parsingResult);
            }, commitment);
            return res;
        }

        public async Task<SubscriptionState> SubscribeMatchesAsync(string accountAddress, Action<SubscriptionState, Solana.Unity.Rpc.Messages.ResponseValue<Solana.Unity.Rpc.Models.AccountInfo>, Matches> callback, Commitment commitment = Commitment.Finalized)
        {
            SubscriptionState res = await StreamingRpcClient.SubscribeAccountInfoAsync(accountAddress, (s, e) =>
            {
                Matches parsingResult = null;
                if (e.Value?.Data?.Count > 0)
                    parsingResult = Matches.Deserialize(Convert.FromBase64String(e.Value.Data[0]));
                callback(s, e, parsingResult);
            }, commitment);
            return res;
        }

        public async Task<SubscriptionState> SubscribeUserAsync(string accountAddress, Action<SubscriptionState, Solana.Unity.Rpc.Messages.ResponseValue<Solana.Unity.Rpc.Models.AccountInfo>, User> callback, Commitment commitment = Commitment.Finalized)
        {
            SubscriptionState res = await StreamingRpcClient.SubscribeAccountInfoAsync(accountAddress, (s, e) =>
            {
                User parsingResult = null;
                if (e.Value?.Data?.Count > 0)
                    parsingResult = User.Deserialize(Convert.FromBase64String(e.Value.Data[0]));
                callback(s, e, parsingResult);
            }, commitment);
            return res;
        }

        public async Task<SubscriptionState> SubscribeVaultAsync(string accountAddress, Action<SubscriptionState, Solana.Unity.Rpc.Messages.ResponseValue<Solana.Unity.Rpc.Models.AccountInfo>, Vault> callback, Commitment commitment = Commitment.Finalized)
        {
            SubscriptionState res = await StreamingRpcClient.SubscribeAccountInfoAsync(accountAddress, (s, e) =>
            {
                Vault parsingResult = null;
                if (e.Value?.Data?.Count > 0)
                    parsingResult = Vault.Deserialize(Convert.FromBase64String(e.Value.Data[0]));
                callback(s, e, parsingResult);
            }, commitment);
            return res;
        }

        public async Task<RequestResult<string>> SendInitializeUserAsync(InitializeUserAccounts accounts, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.KamikazeJoeProgram.InitializeUser(accounts, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendInitializeGameAsync(InitializeGameAccounts accounts, byte? width, byte? height, byte? arenaSeed, ulong? pricePoolLamports, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.KamikazeJoeProgram.InitializeGame(accounts, width, height, arenaSeed, pricePoolLamports, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendInitializeAsync(InitializeAccounts accounts, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.KamikazeJoeProgram.Initialize(accounts, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendJoinGameAsync(JoinGameAccounts accounts, byte x, byte y, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.KamikazeJoeProgram.JoinGame(accounts, x, y, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendMakeMoveAsync(MakeMoveAccounts accounts, Facing direction, byte energy, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.KamikazeJoeProgram.MakeMove(accounts, direction, energy, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendExplodeAsync(ExplodeAccounts accounts, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.KamikazeJoeProgram.Explode(accounts, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendClaimPrizeAsync(ClaimPrizeAccounts accounts, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.KamikazeJoeProgram.ClaimPrize(accounts, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendClaimPrizeSoarAsync(ClaimPrizeSoarAccounts accounts, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.KamikazeJoeProgram.ClaimPrizeSoar(accounts, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendInitializeLeaderboardAsync(InitializeLeaderboardAccounts accounts, PublicKey game, PublicKey leaderboard, PublicKey topEntries, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.KamikazeJoeProgram.InitializeLeaderboard(accounts, game, leaderboard, topEntries, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        protected override Dictionary<uint, ProgramError<KamikazeJoeErrorKind>> BuildErrorsDictionary()
        {
            return new Dictionary<uint, ProgramError<KamikazeJoeErrorKind>>{{6000U, new ProgramError<KamikazeJoeErrorKind>(KamikazeJoeErrorKind.InvalidSize, "Invalid Grid size")}, {6001U, new ProgramError<KamikazeJoeErrorKind>(KamikazeJoeErrorKind.GameEnded, "Unable to join a game that ended")}, {6002U, new ProgramError<KamikazeJoeErrorKind>(KamikazeJoeErrorKind.PlayerNotFound, "Player is not part of this game")}, {6003U, new ProgramError<KamikazeJoeErrorKind>(KamikazeJoeErrorKind.NotValidEnergy, "Energy is not a valid value")}, {6004U, new ProgramError<KamikazeJoeErrorKind>(KamikazeJoeErrorKind.MovingIntoNotEmptyCell, "Unable to move into a not empty cell")}, {6005U, new ProgramError<KamikazeJoeErrorKind>(KamikazeJoeErrorKind.InvalidMovement, "This movement is not valid")}, {6006U, new ProgramError<KamikazeJoeErrorKind>(KamikazeJoeErrorKind.InvalidJoin, "This position is not valid for joining the game")}, {6007U, new ProgramError<KamikazeJoeErrorKind>(KamikazeJoeErrorKind.InvalidClaim, "Price can't be claimed")}, {6008U, new ProgramError<KamikazeJoeErrorKind>(KamikazeJoeErrorKind.Overflow, "Invalid Operation")}, {6009U, new ProgramError<KamikazeJoeErrorKind>(KamikazeJoeErrorKind.InvalidUser, "Invalid User")}, {6010U, new ProgramError<KamikazeJoeErrorKind>(KamikazeJoeErrorKind.InvalidAuthority, "Player key does not match user authority")}, };
        }
    }

    namespace Program
    {
        public class InitializeUserAccounts
        {
            public PublicKey Payer { get; set; }

            public PublicKey User { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public class InitializeGameAccounts
        {
            public PublicKey Creator { get; set; }

            public PublicKey User { get; set; }

            public PublicKey Game { get; set; }

            public PublicKey Matches { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public class InitializeAccounts
        {
            public PublicKey Payer { get; set; }

            public PublicKey Matches { get; set; }

            public PublicKey Vault { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public class JoinGameAccounts
        {
            public PublicKey Player { get; set; }

            public PublicKey User { get; set; }

            public PublicKey Game { get; set; }

            public PublicKey Vault { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public class MakeMoveAccounts
        {
            public PublicKey Payer { get; set; }

            public PublicKey User { get; set; }

            public PublicKey Game { get; set; }

            public PublicKey SessionToken { get; set; }
        }

        public class ExplodeAccounts
        {
            public PublicKey Payer { get; set; }

            public PublicKey User { get; set; }

            public PublicKey Game { get; set; }

            public PublicKey SessionToken { get; set; }
        }

        public class ClaimPrizeAccounts
        {
            public PublicKey Payer { get; set; }

            public PublicKey Receiver { get; set; }

            public PublicKey User { get; set; }

            public PublicKey Game { get; set; }

            public PublicKey Vault { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public class ClaimPrizeSoarAccounts
        {
            public PublicKey Payer { get; set; }

            public PublicKey Receiver { get; set; }

            public PublicKey User { get; set; }

            public PublicKey Game { get; set; }

            public PublicKey Vault { get; set; }

            public PublicKey LeaderboardInfo { get; set; }

            public PublicKey SoarGame { get; set; }

            public PublicKey SoarLeaderboard { get; set; }

            public PublicKey SoarPlayerAccount { get; set; }

            public PublicKey SoarPlayerScores { get; set; }

            public PublicKey SoarTopEntries { get; set; }

            public PublicKey SoarProgram { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public class InitializeLeaderboardAccounts
        {
            public PublicKey Payer { get; set; }

            public PublicKey Leaderboard { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public static class KamikazeJoeProgram
        {
            public static Solana.Unity.Rpc.Models.TransactionInstruction InitializeUser(InitializeUserAccounts accounts, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Payer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.User, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(18313459337071759727UL, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction InitializeGame(InitializeGameAccounts accounts, byte? width, byte? height, byte? arenaSeed, ulong? pricePoolLamports, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Creator, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.User, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Game, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Matches == null ? programId : accounts.Matches, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(15529203708862021164UL, offset);
                offset += 8;
                if (width != null)
                {
                    _data.WriteU8(1, offset);
                    offset += 1;
                    _data.WriteU8(width.Value, offset);
                    offset += 1;
                }
                else
                {
                    _data.WriteU8(0, offset);
                    offset += 1;
                }

                if (height != null)
                {
                    _data.WriteU8(1, offset);
                    offset += 1;
                    _data.WriteU8(height.Value, offset);
                    offset += 1;
                }
                else
                {
                    _data.WriteU8(0, offset);
                    offset += 1;
                }

                if (arenaSeed != null)
                {
                    _data.WriteU8(1, offset);
                    offset += 1;
                    _data.WriteU8(arenaSeed.Value, offset);
                    offset += 1;
                }
                else
                {
                    _data.WriteU8(0, offset);
                    offset += 1;
                }

                if (pricePoolLamports != null)
                {
                    _data.WriteU8(1, offset);
                    offset += 1;
                    _data.WriteU64(pricePoolLamports.Value, offset);
                    offset += 8;
                }
                else
                {
                    _data.WriteU8(0, offset);
                    offset += 1;
                }

                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction Initialize(InitializeAccounts accounts, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Payer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Matches, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Vault, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(17121445590508351407UL, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction JoinGame(JoinGameAccounts accounts, byte x, byte y, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Player, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.User, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Game, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Vault, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(9240450992125931627UL, offset);
                offset += 8;
                _data.WriteU8(x, offset);
                offset += 1;
                _data.WriteU8(y, offset);
                offset += 1;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction MakeMove(MakeMoveAccounts accounts, Facing direction, byte energy, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Payer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.User, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Game, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SessionToken == null ? programId : accounts.SessionToken, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(16848199159844982094UL, offset);
                offset += 8;
                _data.WriteU8((byte)direction, offset);
                offset += 1;
                _data.WriteU8(energy, offset);
                offset += 1;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction Explode(ExplodeAccounts accounts, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Payer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.User, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Game, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SessionToken == null ? programId : accounts.SessionToken, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(17851550339501723000UL, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction ClaimPrize(ClaimPrizeAccounts accounts, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Payer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Receiver == null ? programId : accounts.Receiver, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.User, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Game, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Vault, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(16999468971785447837UL, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction ClaimPrizeSoar(ClaimPrizeSoarAccounts accounts, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Payer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Receiver == null ? programId : accounts.Receiver, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.User, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Game, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Vault, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.LeaderboardInfo, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SoarGame, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SoarLeaderboard, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SoarPlayerAccount, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.SoarPlayerScores, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.SoarTopEntries, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SoarProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(8161990115184007846UL, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction InitializeLeaderboard(InitializeLeaderboardAccounts accounts, PublicKey game, PublicKey leaderboard, PublicKey topEntries, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Payer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Leaderboard, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(12707869719036827439UL, offset);
                offset += 8;
                _data.WritePubKey(game, offset);
                offset += 32;
                _data.WritePubKey(leaderboard, offset);
                offset += 32;
                _data.WritePubKey(topEntries, offset);
                offset += 32;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }
        }
    }
}