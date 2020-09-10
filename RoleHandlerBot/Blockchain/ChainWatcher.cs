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
        public static string web3Url = "https://mainnet.infura.io/v3/b4e2781f02a94a5a96dcf8ce8cab9449";

        public static async Task<BigInteger> GetBalanceOf(string tokenAddress, string owner)
        {
            Web3 web3;
            web3 = new Web3("https://mainnet.infura.io/v3/b4e2781f02a94a5a96dcf8ce8cab9449");
            var handler = web3.Eth.GetContractQueryHandler<BalanceOfFunction>();
            var param = new BalanceOfFunction() { Owner = owner };
            var balance = await handler.QueryAsync<BigInteger>(tokenAddress, param);
            return balance;
        }

        public static async Task<BigInteger> GetTokenDecimal(string tokenAddress)
        {
            Web3 web3;
            web3 = new Web3("https://mainnet.infura.io/v3/b4e2781f02a94a5a96dcf8ce8cab9449");
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

    [Function("decimals", "uint128")]
    public class DecimalsFunction : FunctionMessage
    {
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
