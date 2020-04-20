using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

// ReSharper disable StringLiteralTypo

namespace Jiamiao.x.RedisConnectSample
{
    public class CsRedisCoreSentinel
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<CsRedisCoreSentinel> _logger;

        public CsRedisCoreSentinel(IConfiguration configuration,ILogger<CsRedisCoreSentinel> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<CSRedisClient> Writer()
        {
            await Task.CompletedTask;
            var strRedisEndPoints = _configuration["DIMSUM_REDISENDPOINT"];
            var redisEndPoints = strRedisEndPoints.Split('|').ToArray();
            _logger.LogInformation($"尝试连接Redis:{JsonConvert.SerializeObject(redisEndPoints)}");
            var csRedis = new CSRedisClient(
                    connectionString: $"{_configuration["DIMSUM_MASTERSERVICENAME"]},password={_configuration["DIMSUM_REDISPWD"]}",
                    sentinels: redisEndPoints,
                    readOnly: false);
            RedisHelper.Initialization(csRedis);
            return RedisHelper.Instance;
        }

        public async Task<CSRedisClient> Reader()
        {
            await Task.CompletedTask;
            var strRedisEndPoints = _configuration["DIMSUM_REDISENDPOINT"];
            var redisEndPoints = strRedisEndPoints.Split('|').ToArray();
            _logger.LogInformation($"尝试连接Redis:{JsonConvert.SerializeObject(redisEndPoints)}");
            var csRedis = new CSRedisClient(
                connectionString: $"{_configuration["DIMSUM_MASTERSERVICENAME"]},password={_configuration["DIMSUM_REDISPWD"]}",
                sentinels: redisEndPoints,
                readOnly: true);
            RedisHelper.Initialization(csRedis);
            return RedisHelper.Instance;
        }
    }
}
