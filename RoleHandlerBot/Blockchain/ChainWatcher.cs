using System;
using System.Threading.Tasks;
using Nethereum.Web3;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Contracts;
using System.Numerics;
using Discord;
using RoleHandlerBot.Mongo;
using MongoDB.Driver;
namespace RoleHandlerBot.Blockchain
{
    public class ChainWatcher
    {
        public static string INFURA_WEB3_ENDPOINT = "https://mainnet.infura.io/v3/b4e2781f02a94a5a96dcf8ce8cab9449";
        public static string GETH_WEB3_ENDPOINT = "http://localhost:8545";


        public static async Task<Web3> GetGoodWeb3() {
            if (await IsGethSynced())
                return new Web3(GETH_WEB3_ENDPOINT);
            return new Web3(INFURA_WEB3_ENDPOINT);
        }

        public static async Task<string> GetOwnerOf(string tokenAddress, BigInteger id) {
            Web3 web3;
            web3 = new Web3(GETH_WEB3_ENDPOINT);
            var handler = web3.Eth.GetContractQueryHandler<OwnerOfFunction>();
            var param = new OwnerOfFunction() { Id = id };
            var balance = await handler.QueryAsync<string>(tokenAddress, param);
            return balance;
        }

        public static async Task<BigInteger> GetBalanceOf(string tokenAddress, string owner)
        {
            Web3 web3;
            web3 = new Web3(GETH_WEB3_ENDPOINT);
            var handler = web3.Eth.GetContractQueryHandler<BalanceOfFunction>();
            var param = new BalanceOfFunction() { Owner = owner };
            var balance = await handler.QueryAsync<BigInteger>(tokenAddress, param);
            return balance;
        }

        public static async Task<BigInteger> GetTokenDecimal(string tokenAddress)
        {
            Web3 web3;
            web3 = new Web3(GETH_WEB3_ENDPOINT);
            var handler = web3.Eth.GetContractQueryHandler<DecimalsFunction>();
            var dec = await handler.QueryAsync<BigInteger>(tokenAddress);
            return dec;
        }
        private static async Task<BlockParameter> GetLastBlockCheckpoint(Web3 web3)
        {
            var lastBlock = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            var blockNumber = lastBlock.Value - 4;
            return new BlockParameter(new HexBigInteger(blockNumber));
        }

        public static async Task<bool> CheckIfKoArtist(string add) {
            Web3 web3;
            web3 = new Web3(GETH_WEB3_ENDPOINT);
            var handler = web3.Eth.GetContractQueryHandler<IsEnabledForAccount>();
            var param = new IsEnabledForAccount() { Artist = add };
            var res = await handler.QueryAsync<bool>("0xec133df5d806a9069aee513b8be01eeee2f03ff0", param);
            return res;
        }

        public static async Task<(BigInteger, BigInteger)> GetBlocks() {
            Web3 web3 = new Web3(INFURA_WEB3_ENDPOINT);
            Web3 web3Geth = new Web3(GETH_WEB3_ENDPOINT);

            var currentBlock = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            var currentBlockGeth = await web3Geth.Eth.Blocks.GetBlockNumber.SendRequestAsync();

            return (currentBlock.Value, currentBlockGeth.Value);
        }

        public static async Task<bool> IsGethSynced() {
            Web3 web3 = new Web3(INFURA_WEB3_ENDPOINT);
            Web3 web3Geth = new Web3(GETH_WEB3_ENDPOINT);

            var currentBlock = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            var currentBlockGeth = await web3Geth.Eth.Blocks.GetBlockNumber.SendRequestAsync();

            return currentBlock.Value - currentBlockGeth.Value < 20;
        }
    }

    [FunctionOutput]
    public class AvastarCount : IFunctionOutputDTO
    {
        [Parameter("uint256", "", 1)]
        public BigInteger Balance { get; set; }
    }

    [Function("totalSupply", "uint256")]
    public class totalSupplyFunction : FunctionMessage
    {
    }
    [Function("balanceOf", "uint256")]
    public class BalanceOfFunction : FunctionMessage
    {
        [Parameter("address")]
        public string Owner { get; set; }
    }

    [Function("ownerOf", "address")]
    public class OwnerOfFunction : FunctionMessage {
        [Parameter("uint256")]
        public BigInteger Id { get; set; }
    }

    [Function("decimals", "uint128")]
    public class DecimalsFunction : FunctionMessage
    {
    }

    [Function("isEnabledForAccount", "bool")]
    public class IsEnabledForAccount : FunctionMessage {
        [Parameter("address")]
        public string Artist { get; set; }
    }

    public class Checkpoint
    {
        public int id;
        public int lastBlockChecked;
        public Checkpoint(int _id, int _lastBlockChecked)
        {
            id = _id;
            lastBlockChecked = _lastBlockChecked;
        }
    }
}
